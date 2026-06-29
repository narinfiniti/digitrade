local cjson = require "cjson.safe"
local http = require "resty.http"

local kong = kong
local ngx = ngx

local IdentityIntrospectionHandler = {
  VERSION = "1.0.0",
  PRIORITY = 1260,
}

local function trim(value)
  if value == nil then
    return nil
  end

  local normalized = tostring(value):match("^%s*(.-)%s*$")
  if normalized == "" then
    return nil
  end

  return normalized
end

local function join_scopes(scopes)
  if type(scopes) ~= "table" or #scopes == 0 then
    return nil
  end

  return table.concat(scopes, " ")
end

local function build_problem(status, title, detail, code)
  return {
    status = status,
    title = title,
    detail = detail,
    code = code,
  }
end

local function ensure_correlation_id(conf)
  local correlation_id = trim(kong.request.get_header(conf.correlation_header_name))
  if correlation_id == nil then
    correlation_id = trim(ngx.var.request_id)
  end

  kong.service.request.set_header(conf.correlation_header_name, correlation_id)
  kong.response.set_header(conf.correlation_header_name, correlation_id)

  return correlation_id
end

local function exit_problem(conf, correlation_id, status, title, detail, code, www_authenticate)
  local headers = {
    [conf.correlation_header_name] = correlation_id,
    ["Content-Type"] = "application/problem+json",
  }

  if www_authenticate ~= nil then
    headers["WWW-Authenticate"] = www_authenticate
  end

  return kong.response.exit(status, build_problem(status, title, detail, code), headers)
end

local function extract_bearer_token()
  local authorization_header = trim(kong.request.get_header("authorization"))
  if authorization_header == nil then
    return nil
  end

  if string.len(authorization_header) < 8 or string.lower(string.sub(authorization_header, 1, 7)) ~= "bearer " then
    return nil
  end

  return trim(string.sub(authorization_header, 8))
end

local function clear_forwarded_headers(conf)
  kong.service.request.clear_header(conf.authenticated_subject_header_name)
  kong.service.request.clear_header(conf.authenticated_user_name_header_name)
  kong.service.request.clear_header(conf.authenticated_scopes_header_name)
end

local function introspect_token(conf, access_token)
  local http_client = http.new()
  http_client:set_timeout(conf.timeout_ms)

  local response, request_error = http_client:request_uri(conf.introspection_url, {
    method = "POST",
    body = cjson.encode({ AccessToken = access_token }),
    headers = {
      ["Content-Type"] = "application/json",
    },
  })

  if response == nil then
    return nil, request_error
  end

  if response.status ~= 200 then
    return nil, "unexpected_status"
  end

  local payload, decode_error = cjson.decode(response.body)
  if payload == nil then
    return nil, decode_error
  end

  if type(payload.data) == "table" then
    return payload.data
  end

  return payload
end

local function apply_identity_context(conf, introspection_result)
  local subject_id = trim(introspection_result.subjectId)
  if subject_id ~= nil then
    kong.service.request.set_header(conf.authenticated_subject_header_name, subject_id)
  end

  local claims = introspection_result.claims
  if type(claims) == "table" then
    local preferred_user_name = trim(claims.preferred_username)
    if preferred_user_name ~= nil then
      kong.service.request.set_header(conf.authenticated_user_name_header_name, preferred_user_name)
    end
  end

  local scopes = join_scopes(introspection_result.scopes)
  if scopes ~= nil then
    kong.service.request.set_header(conf.authenticated_scopes_header_name, scopes)
  end
end

local function authenticate_request(conf)
  clear_forwarded_headers(conf)

  local correlation_id = ensure_correlation_id(conf)
  local access_token = extract_bearer_token()
  if access_token == nil then
    return exit_problem(
      conf,
      correlation_id,
      401,
      "Authentication required",
      "A valid bearer token is required before ApiGateway can route the request.",
      "gateway.authentication.missing_bearer_token",
      "Bearer")
  end

  local introspection_result, introspection_error = introspect_token(conf, access_token)
  if introspection_result == nil then
    kong.log.err("identity introspection failed: ", introspection_error)

    return exit_problem(
      conf,
      correlation_id,
      503,
      "IdentityService unavailable",
      "ApiGateway could not reach IdentityService for token introspection.",
      "gateway.identity.unavailable")
  end

  if introspection_result.isActive ~= true then
    return exit_problem(
      conf,
      correlation_id,
      401,
      "Authentication failed",
      "IdentityService reported the supplied bearer token as inactive.",
      "gateway.authentication.invalid_token",
      "Bearer error=\"invalid_token\"")
  end

  apply_identity_context(conf, introspection_result)
end

function IdentityIntrospectionHandler:access(conf)
  return authenticate_request(conf)
end

function IdentityIntrospectionHandler:ws_handshake(conf)
  return authenticate_request(conf)
end

return IdentityIntrospectionHandler
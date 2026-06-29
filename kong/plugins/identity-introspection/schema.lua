local typedefs = require "kong.db.schema.typedefs"

return {
  name = "identity-introspection",
  fields = {
    {
      consumer = typedefs.no_consumer,
    },
    {
      protocols = {
        type = "set",
        required = true,
        default = { "http", "https", "ws", "wss" },
        elements = {
          type = "string",
          one_of = { "http", "https", "ws", "wss" },
        },
      },
    },
    {
      config = {
        type = "record",
        fields = {
          {
            introspection_url = {
              type = "string",
              required = true,
              match = "^https?://",
            },
          },
          {
            correlation_header_name = {
              type = "string",
              default = "X-Correlation-Id",
              len_min = 1,
            },
          },
          {
            authenticated_subject_header_name = {
              type = "string",
              default = "X-Authenticated-Subject",
              len_min = 1,
            },
          },
          {
            authenticated_user_name_header_name = {
              type = "string",
              default = "X-Authenticated-UserName",
              len_min = 1,
            },
          },
          {
            authenticated_scopes_header_name = {
              type = "string",
              default = "X-Authenticated-Scopes",
              len_min = 1,
            },
          },
          {
            timeout_ms = {
              type = "integer",
              default = 3000,
              between = { 100, 60000 },
            },
          },
        },
      },
    },
  },
}
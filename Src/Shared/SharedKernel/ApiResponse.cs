using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedKernel
{
    public class ApiResponse
    {
        [JsonPropertyName("IsSuccess")]
        public bool IsSuccess { get; set; }
        [JsonPropertyName("StatusCode")]
        public HttpStatusCode StatusCode { get; set; }

        [JsonPropertyName("Message")]
        public string? Message { get; set; }

        [JsonPropertyName("Data")]
        public object? Data { get; set; }

        [JsonPropertyName("TotalRecords")]
        public int? TotalRecords { get; set; }

        [JsonPropertyName("TraceId")]
        public string? TraceId { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(string message)
        {
            Message = message;
        }

        public ApiResponse(string message, dynamic data)
            : this(message)
        {
            Data = (object)data;
        }

        public ApiResponse(string message, dynamic? data, int totalRecords)
            : this(message)
        {
            IsSuccess = true;
            Data = data;
            TotalRecords = totalRecords;
            StatusCode = HttpStatusCode.OK;
        }

        public ApiResponse(bool IsSuccess, string message, dynamic? data, int totalRecords)
            : this(message)
        {
            this.IsSuccess = IsSuccess;
            Data = data;
            TotalRecords = totalRecords;
            StatusCode = IsSuccess ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
        }

        public ApiResponse(bool IsSuccess, HttpStatusCode httpStatusCode, string message, dynamic? data, int totalRecords)
            : this(message)
        {
            this.IsSuccess = IsSuccess;
            Data = data;
            TotalRecords = totalRecords;
            StatusCode = httpStatusCode;
        }

        public static ApiResponse SuccessWithData(dynamic? data, int rows)
                    => new(true, "Process Success", data, rows);

        public static ApiResponse Success()
                    => new(true, "Process Success", new { }, 0);

        public static ApiResponse Failure(string message) => new(false, message, null, 0);

        public static ApiResponse NotFound() => new(false, HttpStatusCode.NotFound, "No se encontraron resultados", null, 0);
        public static ApiResponse NotFoundWithMessage(string message) => new(false, HttpStatusCode.NotFound, message, null, 0);
        public static ApiResponse ValidationError(string message) => new(false, HttpStatusCode.BadRequest, message, null, 0);
        public static ApiResponse Failure(string message, List<string> data) => new(false, message, data, 0);
        public static ApiResponse PartialFailure(HttpStatusCode httpStatusCode, string message, dynamic? data, int totalRecords) => new(false, httpStatusCode, message, data, totalRecords);

    }
    
    public static class ApiResponseExtensions
    {
        public static T? GetData<T>(this ApiResponse response)
        {
            if (response.Data is JsonElement json)
                return json.Deserialize<T>();

            return default;
        }
    }
}

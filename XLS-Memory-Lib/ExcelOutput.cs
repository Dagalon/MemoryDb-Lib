namespace XLS_Memory_Lib;

public static class ExcelOutput
{
    public const string Success = "SUCCESS";
    private const string ErrorPrefix = "ERROR: ";

    public static string FromStatus(Enum status, string? successMessage = null)
    {
        return IsSuccess(status) ? successMessage ?? Success : Error(status.ToString());
    }

    public static string FromOperationString(string output)
    {
        if (string.Equals(output, Success, StringComparison.OrdinalIgnoreCase))
        {
            return Success;
        }

        return IsError(output) ? Error(StripErrorPrefix(output)) : Error(output);
    }

    public static string Error(Exception exception)
    {
        return Error(exception.Message);
    }

    public static string Error(string message)
    {
        var cleanMessage = StripErrorPrefix(message);
        return string.IsNullOrWhiteSpace(cleanMessage) ? "ERROR" : ErrorPrefix + cleanMessage;
    }

    public static object[,] ErrorTable(string message)
    {
        return new object[,] { { Error(message) } };
    }

    private static bool IsSuccess(Enum status)
    {
        return string.Equals(status.ToString(), Success, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsError(string message)
    {
        var trimmed = message.TrimStart();
        return trimmed.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripErrorPrefix(string message)
    {
        var output = (message ?? string.Empty).Trim();
        while (output.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            output = output["ERROR".Length..].TrimStart(' ', ':', '-');
        }

        return output;
    }
}

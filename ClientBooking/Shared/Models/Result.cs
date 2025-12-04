namespace ClientBooking.Shared.Models;

//Generic result class that can be used to return a value or an error
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Result(T value, bool isSuccess, string error, Dictionary<string, string[]>? validationErrors = null)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public static Result<T> Failure(string error) => new(default!, false, error);
    public static Result<T> ValidationFailure(Dictionary<string, string[]> errors) => new(default!, false, "Validation failed", errors);
}
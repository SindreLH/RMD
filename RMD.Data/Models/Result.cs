namespace RMD.Data.Models
{
    public class Result
    {

        // Class instance variables
        public bool IsSuccess { get; }
        public string Error { get; }


        // Constructor used to init isSuccess and error
        protected Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        //Methods returning success/failure objects. They Set isSuccess=true and an error message respectively
        public static Result Success() => new Result(true, string.Empty);
        public static Result Failure(string error) => new Result(false, error);
    }

    // Extending the base class of Result - makes it possible to carry a value of a generic type T (int, string, object etc.) when operation is successful
    public class Result<T> : Result
    {
        public T Value { get; }

        protected Result(bool isSuccess, T value, string error) : base(isSuccess, error) {
            Value = value;
		}

        // Static factory methods which create a Result<T> objects based on successful/failed operations. The result objects carry the value of Type<T>.
        // Method 1 will call the constructor with isSuccess set to TRUE if operation is SUCCESSFUL and error message set to an EMPTY STRING.
        // Method 2 will call the constructor with isSuccess set to FALSE if operations FAILS and will include ERROR MESSAGE.

        public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error);
	}
}

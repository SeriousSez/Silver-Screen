namespace SilverScreen.Domain.Movie
{
    public enum MovieCreationFailure
    {
        None,
        ActiveProductionInProgress
    }

    public sealed class MovieCreationResult
    {
        private MovieCreationResult(MovieProject project, MovieCreationFailure failure, string message)
        {
            Project = project;
            Failure = failure;
            Message = message;
        }

        public bool Succeeded => Project != null && Failure == MovieCreationFailure.None;
        public MovieProject Project { get; }
        public MovieCreationFailure Failure { get; }
        public string Message { get; }

        public static MovieCreationResult Success(MovieProject project)
        {
            return new MovieCreationResult(project, MovieCreationFailure.None, string.Empty);
        }

        public static MovieCreationResult Failed(MovieCreationFailure failure, string message)
        {
            return new MovieCreationResult(null, failure, message);
        }
    }
}

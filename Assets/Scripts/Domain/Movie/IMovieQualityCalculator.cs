namespace SilverScreen.Domain.Movie
{
    public interface IMovieQualityCalculator
    {
        MovieProductionResult Calculate(MovieProject movie);
    }
}

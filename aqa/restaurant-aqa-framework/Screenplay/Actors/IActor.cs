namespace RestaurantAqaFramework.Screenplay.Actors;

public interface IActor
{
    string Name { get; }
    void Can<TAbility>(TAbility ability);
    TAbility Using<TAbility>();
    bool HasAbility<TAbility>();

    Task AttemptsTo(params IPerformable[] tasks);
    Task<TAnswer> AsksFor<TAnswer>(IQuestion<TAnswer> question);
}

public interface IPerformable
{
    Task PerformAs(IActor actor);
}

public interface IQuestion<TAnswer>
{
    Task<TAnswer> AnsweredBy(IActor actor);
}


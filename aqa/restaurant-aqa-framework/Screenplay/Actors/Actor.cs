using System.Collections.Concurrent;

namespace RestaurantAqaFramework.Screenplay.Actors;

public class Actor : IActor
{
    private readonly ConcurrentDictionary<Type, object> _abilities = new();

    public string Name { get; }

    public Actor(string name)
    {
        Name = name;
    }

    public static Actor Named(string name) => new(name);

    public void Can<TAbility>(TAbility ability)
    {
        if (ability == null) throw new ArgumentNullException(nameof(ability));
        _abilities[typeof(TAbility)] = ability;
    }

    public TAbility Using<TAbility>()
    {
        if (_abilities.TryGetValue(typeof(TAbility), out var ability))
        {
            return (TAbility)ability;
        }

        throw new InvalidOperationException($"Actor '{Name}' does not have the ability '{typeof(TAbility).Name}'.");
    }

    public bool HasAbility<TAbility>() => _abilities.ContainsKey(typeof(TAbility));

    public async Task AttemptsTo(params IPerformable[] tasks)
    {
        foreach (var task in tasks)
        {
            await task.PerformAs(this);
        }
    }

    public async Task<TAnswer> AsksFor<TAnswer>(IQuestion<TAnswer> question)
    {
        return await question.AnsweredBy(this);
    }
}


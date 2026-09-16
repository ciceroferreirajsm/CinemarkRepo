namespace MovieCatalog.Domain.Entities;

/// <summary>
/// Marker contract used by the generic repository so it can build id-based
/// queries (Builders&lt;T&gt;.Filter.Eq(x =&gt; x.Id, ...)) without depending on any
/// specific entity type.
/// </summary>
public interface IEntity
{
    string Id { get; set; }
}

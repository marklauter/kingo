
namespace Kingo.Storage;

public interface IDocumentWriter<D>
{
    Task Insert(D document, CancellationToken cancellationToken);

    Task InsertOrUpdate(D document, CancellationToken cancellationToken);

    Task Update(D document, CancellationToken cancellationToken);
}

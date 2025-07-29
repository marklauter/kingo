using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<D>
{
    Eff<Unit> Insert(D document);
    Eff<Unit> InsertOrUpdate(D document);
    Eff<Unit> Update(D document);
}

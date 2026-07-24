using Authorization2.Domain;

namespace Authorization2.Mappers;

public interface IMapper<T,B>
{
    public Result<B> Map(T  source);
}
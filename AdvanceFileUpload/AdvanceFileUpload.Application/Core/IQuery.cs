using MediatR;

namespace AdvanceFileUpload.Application.Core
{
    public interface IQuery<out TResult> : IRequest<TResult>
    { }

}

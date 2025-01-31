using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace AdvanceFileUpload.Application.Core
{
    public interface ICommand<out TResult> : IRequest<TResult>
    { 
    }

}

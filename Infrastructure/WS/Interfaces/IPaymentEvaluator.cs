using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.WS.Interfaces
{
    public interface IPaymentEvaluator
    {
        bool InProgress { get; }

        void InitProcess();

        void StopProcess();
    }
}

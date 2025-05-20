using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QABrokerAPI.Zerodha
{
    public interface IAPIProcessor
    {
        void SetCacheTime(TimeSpan time);

        Task<T> ProcessGetRequest<T>(BrokerEndpointData endpoint, int receiveWindow = 5000) where T : class;

        Task<T> ProcessDeleteRequest<T>(BrokerEndpointData endpoint, int receiveWindow = 5000) where T : class;

        Task<T> ProcessPostRequest<T>(BrokerEndpointData endpoint, int receiveWindow = 5000) where T : class;

        Task<T> ProcessPutRequest<T>(BrokerEndpointData endpoint, int receiveWindow = 5000) where T : class;
    }
}

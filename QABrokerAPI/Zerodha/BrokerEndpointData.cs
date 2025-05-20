using QABrokerAPI.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace QABrokerAPI.Zerodha
{
    public class BrokerEndpointData
    {
        public Uri Uri;
        public EndpointSecurityType SecurityType;

        public bool UseCache { get; }

        public BrokerEndpointData(Uri uri, EndpointSecurityType securityType, bool useCache = false)
        {
            this.Uri = uri;
            this.SecurityType = securityType;
            this.UseCache = useCache;
        }

        public override string ToString() => this.Uri.AbsoluteUri;
    }
}

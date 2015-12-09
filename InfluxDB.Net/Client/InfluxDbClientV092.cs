using InfluxDB.Net.Contracts;
using InfluxDB.Net.Enums;
using InfluxDB.Net.Infrastructure.Configuration;
using InfluxDB.Net.Infrastructure.Formatters;

namespace InfluxDB.Net
{
    internal class InfluxDbClientV092 : InfluxDbClient
    {
        public InfluxDbClientV092(InfluxDbClientConfiguration configuration) 
            : base(configuration)
        {            
        }

        public override IFormatter GetFormatter()
        {
            return new FormatterV092();
        }

        public override InfluxVersion GetVersion()
        {
            return InfluxVersion.v092;
        }
    }
}
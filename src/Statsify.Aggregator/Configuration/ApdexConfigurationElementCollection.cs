using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Statsify.Aggregator.Configuration
{
    [ConfigurationCollection(typeof(DownsampleConfigurationElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ApdexConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<ApdexConfigurationElement>
    {
        public ApdexConfigurationElement this[int index]
        {
            get { return (ApdexConfigurationElement)BaseGet(index); }
            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ApdexConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ApdexConfigurationElement)element).Metric;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "apdex"; }
        }

        IEnumerator<ApdexConfigurationElement> IEnumerable<ApdexConfigurationElement>.GetEnumerator()
        {
            return this.OfType<ApdexConfigurationElement>().GetEnumerator();
        }
    }
}

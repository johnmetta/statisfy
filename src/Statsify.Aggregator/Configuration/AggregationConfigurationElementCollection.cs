using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Statsify.Aggregator.Configuration
{
    [ConfigurationCollection(typeof(AggregationConfigurationElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class AggregationConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<AggregationConfigurationElement>
    {
        public AggregationConfigurationElement this[int index]
        {
            get { return (AggregationConfigurationElement)BaseGet(index); }
            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                base.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AggregationConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AggregationConfigurationElement)element).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "aggregate"; }
        }

        IEnumerator<AggregationConfigurationElement> IEnumerable<AggregationConfigurationElement>.GetEnumerator()
        {
            return this.Cast<AggregationConfigurationElement>().GetEnumerator();
        }
    }
}
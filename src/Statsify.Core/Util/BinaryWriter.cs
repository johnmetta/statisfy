using System.IO;
using System.Text;
using MiscUtil.IO;

namespace Statsify.Core.Util
{
    public class BinaryWriter : System.IO.BinaryWriter
    {
        public BinaryWriter(Stream input, Encoding encoding, bool leaveOpen) :
            base(leaveOpen ? new NonClosingStreamWrapper(input) : input, encoding)
        {
        }
    }
}
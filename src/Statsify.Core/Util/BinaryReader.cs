using System.IO;
using System.Text;
using MiscUtil.IO;

namespace Statsify.Core.Util
{
    public class BinaryReader : System.IO.BinaryReader
    {
        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen) :
            base(leaveOpen ? new NonClosingStreamWrapper(input) : input, encoding)
        {
        }
    }
}

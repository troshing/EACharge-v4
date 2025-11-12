using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace EACharge
{
    internal class EATestUnit
    {
        uint[] utuints = new uint[5] { 0x6B724D69, 0x525A6F53, 0x39372D31, 0x2E202076, 0x3200392E };
        ushort[] utshorts = new ushort[20];
        public string testString { get; set; }


        public EATestUnit() 
        {
            //SetTestData();
        }

        public void SetTestData()
        {
            byte[] byteString = new byte[utuints.Length * 4];

            int j = 0;
            for (var i = 0; i < utuints.Length; i++)
            {
                byte[] oneUint =  BitConverter.GetBytes(utuints[i]);
                Array.Copy(oneUint, 0, byteString, j,oneUint.Length);
                j += 4;
            }             
        }

        public void TransformArray()
        {

        }

        public void SetTextArray()
        {
            byte[] byteString = Converter.ConvertUintArrayToByteArray(utuints);
            testString = Encoding.Default.GetString(byteString);
            // 

        }
    }
}

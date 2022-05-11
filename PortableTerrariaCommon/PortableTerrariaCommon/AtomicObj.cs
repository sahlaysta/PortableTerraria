using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //thread safe atomic obj
    class AtomicObj<T>
    {
        //constructor
        public AtomicObj(T obj = default)
        {
            val = obj;
        }

        //public operations
        public T Value
        {
            get
            {
                lock (objLock)
                {
                    return (T)val;
                }
            }
            set
            {
                lock (objLock)
                {
                    val = value;
                }
            }
        }

        volatile object val;
        readonly object objLock = new object();
    }
}

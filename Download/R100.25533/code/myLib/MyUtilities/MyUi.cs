using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyUtilities
{
    class MyUi
    {
        public static string TraceClass;

        public MyUi()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }

        public void InvokeIfRequired(Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new Action(() => action()));
            }
            else
            {
                action();
            }
        }
    }
}

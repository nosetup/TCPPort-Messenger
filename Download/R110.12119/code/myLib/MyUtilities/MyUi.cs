using System;
using System.Windows.Forms;

namespace MyUtilities
{
    class MyUi
    {
        #region Local Variables
        public static string TraceClass;
        #endregion

        #region Constructor
        public MyUi()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }
        #endregion

        #region Local Methods
        /// <summary>
        /// Execute an action on a UI control's thread if it's required</summary>
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
        #endregion // Local Methods
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace reika.Core
{
    public interface IOperationEntryUI
    {
        void SetTextPrimary(string title);
        void SetTextSecondary(string title);
        void SetTextSecondary2(string title);
        void SetProgress(double progress);

        void UpdateProgressBasedOnYTDLPLine(string line);
    }
}

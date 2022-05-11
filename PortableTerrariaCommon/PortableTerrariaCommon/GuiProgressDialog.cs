using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    // dialog with label, progress bar, and cancel button
    class GuiProgressDialog : Form
    {
        //constructor
        public GuiProgressDialog(string labelText, string title)
        {
            init(labelText);
            Text = title;

            //so that the "cancel" button isnt the default focus
            GuiHelper.RunWhenShown(this, () => label.Focus());
        }

        //public operations
        public ProgressBar ProgressBar { get { return progressBar; } }
        public event EventHandler UserCanceled;

        //progress bar
        public void SetProgress(int numerator, int denominator)
        {
            //avoid division by 0
            if (denominator == 0)
            {
                numerator = 0;
                denominator = 1;
            }
            
            //avoid overflow
            if (numerator > denominator)
            {
                denominator = numerator;
            }

            //when canceled
            if (userCanceled)
                return;

            //progress bar and percentage label
            progressVal = numerator;
            progressMax = denominator;
        }

        //finish progress
        public void FinishProgress(bool cancel = false)
        {
            //stop progress bar update timer
            stopTimer();

            //finished
            finished = true;

            //end progress shows 100%
            progressVal = cancel ? 0 : 1;
            progressMax = 1;
            progressUpdate();

            //button switch
            buttonPanel.CancelButton.Enabled = false;
            buttonPanel.OkButton.Enabled = true;
            buttonPanel.OkButton.Focus();

            //label for cancel / error
            if (cancel)
                progressLabel.Text = "Stopped";

            //close when done
            if (closeWhenDone)
                Close();
        }

        //cancel
        public void CancelProgress() => FinishProgress(true);

        //disable top right X close button
        protected override CreateParams CreateParams
        {
            get
            {
                const int CP_NOCLOSE_BUTTON = 0x200;
                var cp = base.CreateParams;
                cp.ClassStyle = cp.ClassStyle | CP_NOCLOSE_BUTTON;
                return cp;
            }
        }

        //initializations
        void init(string labelText)
        {
            //dialog
            GuiHelper.SetDialogProperties(this);

            //build main panel
            var pb = new GuiPanelBuilder(this);

            //label
            label = new Label()
            {
                AutoSize = true,
                MaximumSize = new Size(pb.Panel.Width, 0),
                Text = labelText
            };
            pb.AddControl(label);

            //progress bar
            progressBar = new ProgressBar();
            pb.AddControl(progressBar);

            //progress label
            progressLabel = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Loading..."
            };
            //SetProgress(0, 0);
            pb.AddControl(progressLabel);

            //buttons
            buttonPanel = new GuiPanelBuilder.OkCancelButtonPanel("Done");
            buttonPanel.OkButton.Enabled = false;
            pb.AddControl(buttonPanel);

            //finish main panel
            pb.Finish();

            //button actions
            buttonPanel.CancelButton.Click += (o, e) =>
            {
                cancelProgress();
            };
            buttonPanel.OkButton.Click += (o, e) =>
            {
                Close();
            };

            //cancel on form close
            FormClosing += (o, e) =>
            {
                if (!finished)
                {
                    e.Cancel = true;
                    closeWhenDone = true;
                    cancelProgress();
                }
            };

            //update progressbar timer
            progressTimer = new Timer()
            {
                Interval = 100
            };
            progressTimer.Tick += (o, e) => timerTick();
            progressTimer.Start();
            Disposed += (o, e) => progressTimer.Dispose();
        }

        //stop the timer
        void stopTimer()
        {
            lock (timerStopped)
            {
                timerStopped.Value = true;
                progressTimer.Stop();
                progressTimer.Dispose();
            }
        }

        //on each timer tick
        void timerTick()
        {
            lock (timerStopped)
            {
                if (!timerStopped.Value)
                    progressUpdate();
            }
        }

        //control update
        void progressUpdate()
        {
            //label percentage text
            double progress = (double)progressVal / progressMax * 100.0;
            int percent = (int)(progress + 0.5);//round up

            //percentage text
            bool hold99 = percent == 100 && !finished;
            if (hold99)//only show 100% when done
                percent = 99;
            string progressLabelText = percent + "%";

            //if progress hasnt passed 0% yet, show "Loading..."
            if (!hasPassedZeroPercent && percent != 0)
            {
                hasPassedZeroPercent = true;
            }
            if (hasPassedZeroPercent)
            {
                progressLabel.Text = progressLabelText;
            }
            else
            {
                progressLabel.Text = "Loading...";
            }

            //progressbar value
            if (!hold99)
            {
                progressBar.Maximum = progressMax;
                progressBar.Value = progressVal;
            }
        }

        //user canceled progress
        void cancelProgress()
        {
            if (userCanceled)
                return;
            userCanceled = true;
            stopTimer();
            progressLabel.Text = "Canceling...";
            UserCanceled?.Invoke(null, EventArgs.Empty);
        }


        bool finished = false;
        bool userCanceled = false;
        bool closeWhenDone = false;
        bool hasPassedZeroPercent = false;
        volatile int progressVal = 0, progressMax = 100;
        AtomicBool timerStopped = new AtomicBool();
        Timer progressTimer;
        Label label;
        Label progressLabel;
        GuiPanelBuilder.OkCancelButtonPanel buttonPanel;
        ProgressBar progressBar;

        //atomic bool
        class AtomicBool
        {
            public bool Value { get { return val; } set { val = value; } }
            volatile bool val;
        }
    }
}

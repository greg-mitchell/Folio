using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace Folio
{
    public partial class MainView : Form
    {
        int BLUR_DELAY = 500;     // time between last keypress and a call to update the imagebox
		int AUTOCOMPLETE_SUGGESTIONS = 5;
        Filter filter;
        Timer blurTimer;
        BindingList<Card> viewCardList;
        bool needToSave = false;

        public MainView()
        {
            InitializeComponent();

            filter = new Filter();
            blurTimer = new Timer();
            blurTimer.Tick += new EventHandler(BlurTimer_Tick);

            toolStripStatusLabel1.Text = "";

            viewCardList = new BindingList<Card>();
            dataGridView1.DataSource = viewCardList;
            dataGridView1.RowsAdded += new DataGridViewRowsAddedEventHandler(dataGridView1_RowsAdded);
            dataGridView1.MouseUp += new MouseEventHandler(dataGridView1_MouseUp);
            InitializeDataGridView();
			
			checkBoxW.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxU.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxB.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxR.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxG.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			
            comboBox1.TextChanged += textBox1_TextChanged;

            // application settings
            this.AUTOCOMPLETE_SUGGESTIONS = Properties.Settings.Default.AutocompleteSuggestions;
            this.BLUR_DELAY = Properties.Settings.Default.BlurDelay;
            blurTimer.Interval = this.BLUR_DELAY;

            Rulings.DateCacheExpires = Properties.Settings.Default.CacheExpirationDate;
            Rulings.RulingsCache = new Uri(Properties.Settings.Default.CachePath, (Properties.Settings.Default.CachePathIsRelative ? UriKind.Relative : UriKind.Absolute));
            Rulings.RulingSource = new Uri(Properties.Settings.Default.RulingSourceUrl);
            Rulings.ExpirationInterval = Properties.Settings.Default.CacheExpirationInterval;

        }

        private void InitializeDataGridView()
        {
            columnsContextMenu.Items.Clear();
            // parse settings for columns
            string displayColumnsSetting = Properties.Settings.Default.DisplayColumns;
            
        }

        void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                DataGridView.HitTestInfo hitTest = dataGridView1.HitTest(e.X, e.Y);
                if (hitTest.Type == DataGridViewHitTestType.ColumnHeader)
                {
                    columnsContextMenu.Show(dataGridView1, new Point(e.X, e.Y));
                }
                else if (hitTest.Type == DataGridViewHitTestType.Cell)
                {
                    cellsContextMenu.Show(dataGridView1, new Point(e.X, e.Y));
                }
            }
        }

        void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            
        }

        void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0 
                && dataGridView1.SelectedRows[0].Cells.Count > 0 
                && dataGridView1.SelectedRows[0].Cells[0].Value != null)
            {

                toolStripStatusLabel1.Text = "Fetching image";
                Filter newFilter = new Filter() { CardName = dataGridView1.SelectedRows[0].Cells[0].Value.ToString() };
                ImageGrabber.GetImageUrl(newFilter, UpdatePictureBox_Completed);
            }
            else if (dataGridView1.SelectedCells.Count == 1 
                && dataGridView1.SelectedCells[0].Value != null)
            {
                toolStripStatusLabel1.Text = "Fetching image";
                Filter newFilter = new Filter() { CardName = dataGridView1.SelectedCells[0].Value.ToString() };
                ImageGrabber.GetImageUrl(newFilter, UpdatePictureBox_Completed);
            }
        }

        void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            toolStripStatusLabel1.Text = "Fetching image";
        }

        void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            MessageBox.Show("Error happened " + anError.Context.ToString());

            if (anError.Context == DataGridViewDataErrorContexts.Commit)
            {
                MessageBox.Show("Commit error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.CurrentCellChange)
            {
                MessageBox.Show("Cell change");
            }
            if (anError.Context == DataGridViewDataErrorContexts.Parsing)
            {
                MessageBox.Show("parsing error");
            }
            if (anError.Context == DataGridViewDataErrorContexts.LeaveControl)
            {
                MessageBox.Show("leave control error");
            }

            if ((anError.Exception) is ConstraintException)
            {
                DataGridView view = (DataGridView)sender;
                view.Rows[anError.RowIndex].ErrorText = "an error";
                view.Rows[anError.RowIndex].Cells[anError.ColumnIndex].ErrorText = "an error";

                anError.ThrowException = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            blurTimer.Stop();
            blurTimer.Interval = BLUR_DELAY;
            blurTimer.Start();
        }

        private void BlurTimer_Tick(object sender, EventArgs e)
        {
            blurTimer.Stop();
			toolStripStatusLabel1.Text = "";
			FetchImage();
            //Rulings.GetCardRulings(filter, new RunWorkerCompletedEventHandler(AutocompleteMatching_Completed));
        }
		
		private void FetchImage() 
		{
			toolStripStatusLabel1.Text = "Fetching image\t";
            filter.CardName = comboBox1.Text;
            ImageGrabber.GetImageUrl(filter, UpdatePictureBox_Completed);
		}

        private void UpdatePictureBox_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Updated image";
            Console.WriteLine("Updated Image");
            if((string)e.Result != pictureBoxCard.ImageLocation) pictureBoxCard.ImageLocation = (string)e.Result ;
        }

        private void AutocompleteMatching_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                toolStripStatusLabel1.Text = "Loading cards cancelled";
            }
            else
            {
                toolStripStatusLabel1.Text = "Matched card";

                UpdateAutocomplete((IEnumerable<Card>)e.Result);
            }
        }

		private void UpdateAutocomplete(IEnumerable<Card> matches)
		{
            int selind = comboBox1.SelectedIndex;
            comboBox1.Items.Clear();
            string[] suggestions;
            suggestions = matches.OrderBy(card => card.Name)
                                         .Take(AUTOCOMPLETE_SUGGESTIONS)
                                         .Select(card => card.Name)
                                         .ToArray();
            comboBox1.Items.AddRange(suggestions);
            comboBox1.SelectedIndex = selind;
            foreach (string suggestion in suggestions) Console.WriteLine("Suggesting: {0}", suggestion);
		}
		
		private void FilterCheckbox_CheckedChanged(object sender, EventArgs e) 
		{
			filter.W = this.checkBoxW.Checked;
			filter.U = this.checkBoxU.Checked;
			filter.B = this.checkBoxB.Checked;
			filter.R = this.checkBoxR.Checked;
			filter.G = this.checkBoxG.Checked;
			
			FetchImage();	
		}
		

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(openFileDialog1.FileName))
                {
                    using (FileStream dataStore = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.ReadWrite))
                    {
                        dataStore.Lock(0, dataStore.Length);
                        OpenDataStore();
                    }
                }
                else
                    MessageBox.Show("File not found!");
            }
        }

        private void OpenDataStore()
        {
            
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            

            saveFileDialog1.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Card[]));
                using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                {
                    serializer.Serialize(sw, viewCardList.ToArray());
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            viewCardList = new BindingList<Card>();
            dataGridView1.DataSource = viewCardList;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (viewCardList == null)
            {
                MessageBox.Show("You must create a new view or open a view before editing!");
                return;
            }
			
			Rulings.GetCardRulings(new Filter() { CardName = comboBox1.Text }, AddCard_Completed);
        }

        private void AddCard_Completed(object obj, RunWorkerCompletedEventArgs e)
        {
            IEnumerable<CardRuling> matches = (IEnumerable<CardRuling>)e.Result;
            if (matches != null && matches.Count() > 0)
                viewCardList.Add((Card)matches.First());
        }

        private void comboBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (viewCardList == null)
                {
                    MessageBox.Show("You must create a new view or open a view before editing!");
                    return;
                }

                Rulings.GetCardRulings(new Filter() { CardName = comboBox1.Text }, AddCard_Completed);
            }
        }

        /*
        private DataTable CreateTable(string p)
        {
            DataTable table = new DataTable(p);
            DataColumn column = new DataColumn("Name", typeof(string));
            column.AllowDBNull = false;
            column.MaxLength = 255;
            table.Columns.Add(column);

            column = new DataColumn("Cost", typeof(Cost));
            column.AllowDBNull = true;
        }
         * */
    }
}

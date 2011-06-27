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
        #region Fields and Settings
        //settings
        /// <summary>Time between last keypress and a call to update the imagebox</summary>
        int BLUR_DELAY = 500;

        /// <summary>Number of autocomplete suggestions to display</summary>
		int AUTOCOMPLETE_SUGGESTIONS = 5;

        // Private fields
        /// <summary>Filters the displayed cards</summary>
        Filter filter;

        /// <summary>Timer for pausing before making picture updates</summary>
        Timer blurTimer;

        /// <summary>List for storing the displayed cards and notifying/responding to changes</summary>
        SortableBindingList<CardCollectionCard> viewCardList;

        bool needToSave = false;
        string currentFilePath;

        // fields to assist with sorting
        DataGridViewColumn sortedColumn;
        SortOrder sortDirection = SortOrder.None;
        #endregion

        #region Constructors, Initializations, and Closing
        public MainView()
        {
            InitializeComponent();

            filter = new Filter();
            blurTimer = new Timer();
            blurTimer.Tick += new EventHandler(BlurTimer_Tick);

            toolStripStatusLabel1.Text = "";

            viewCardList = new SortableBindingList<CardCollectionCard>();
            dataGridView1.DataSource = viewCardList;
            dataGridView1.MouseUp += new MouseEventHandler(dataGridView1_MouseUp);
            dataGridView1.CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
            dataGridView1.RowsAdded += new DataGridViewRowsAddedEventHandler(dataGridView1_RowsAdded);
            dataGridView1.DataError +=new DataGridViewDataErrorEventHandler(dataGridView1_DataError);
            dataGridView1.EnableHeadersVisualStyles = true;
            InitializeDataGridView();
			
			checkBoxW.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxU.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxB.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxR.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			checkBoxG.CheckedChanged += new EventHandler(FilterCheckbox_CheckedChanged);
			
            comboBox1.TextChanged += textBox1_TextChanged;

            toolStripStatusLabel1.Text = "Loading card base";
            Rulings.LoadCache();
            toolStripStatusLabel1.Text = "Card base loaded";

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

            string displayColumnsSetting = Properties.Settings.Default.DisplayColumns;
            if (!String.IsNullOrEmpty(displayColumnsSetting))
            {
                string[] displayColumnTitles = displayColumnsSetting.Split(new char[] { ',' });
                if (displayColumnTitles != null)
                {
                    dataGridView1.AutoGenerateColumns = false;
                    dataGridView1.Columns.Clear();
                    foreach (string title in displayColumnTitles)
                    {
                        DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                        column.HeaderText = title;
                        column.DataPropertyName = title;
                        dataGridView1.Columns.Add(column);
                    }
                }
            }
            else
            {
                dataGridView1.AutoGenerateColumns = false;
                dataGridView1.Columns.Clear();

                string[] columns = new string[] { "Quantity", "Name", "Cost", "Color", "Type", "Set", "Condition" };
                Properties.Settings.Default.DisplayColumns = "Name,Cost,Color,Type,Set,Condition";

                DataGridViewCell cellTemplate = new DataGridViewTextBoxCell();

                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Quant", DataPropertyName = "Quantity", SortMode = DataGridViewColumnSortMode.Programmatic });
                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Name", DataPropertyName = "Name", SortMode = DataGridViewColumnSortMode.Programmatic , ReadOnly = true});
                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Cost", DataPropertyName = "Cost" , SortMode = DataGridViewColumnSortMode.Programmatic , ReadOnly=true});
                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Color", DataPropertyName = "Color", SortMode = DataGridViewColumnSortMode.Programmatic, ReadOnly=true});
                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Type", DataPropertyName = "Type" , SortMode=DataGridViewColumnSortMode.Programmatic, ReadOnly=true});
                                
                DataGridViewComboBoxColumn comboBoxCol = new DataGridViewComboBoxColumn() { HeaderText = "Condition", DataPropertyName = "Condition", SortMode=DataGridViewColumnSortMode.NotSortable };
                Array vals = Enum.GetValues(typeof(Condition));
                for(int i=0;i<vals.Length;i++) comboBoxCol.Items.Add(vals.GetValue(i));
                dataGridView1.Columns.Add(comboBoxCol);

                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Set", DataPropertyName = "Set", SortMode = DataGridViewColumnSortMode.Programmatic });
                dataGridView1.Columns.Add(new DataGridViewColumn(cellTemplate) { HeaderText = "Notes", DataPropertyName = "Notes", SortMode = DataGridViewColumnSortMode.NotSortable });
            }
            columnsContextMenu.Items.Clear();
            // parse settings for columns

            needToSave = false;
        }

        private void MainView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (needToSave)
            {
                DialogResult res = MessageBox.Show("Do you want to save changes?", "Folio", MessageBoxButtons.YesNoCancel);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    if (String.IsNullOrEmpty(currentFilePath))
                    {
                        if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK
                            && File.Exists(saveFileDialog1.FileName))
                        {
                            SaveAndUnlock(saveFileDialog1.FileName);
                        }
                        else
                        {
                            MessageBox.Show("File name or path invalid!");
                            e.Cancel = true;
                        }
                    }
                    else
                    {
                        if (File.Exists(currentFilePath))
                        {
                            SaveAndUnlock(currentFilePath);
                        }
                    }
                }
                else if (res == System.Windows.Forms.DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
        #endregion

        #region DataGridView Events and Sorting
        void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                DataGridView.HitTestInfo hitTest = dataGridView1.HitTest(e.X, e.Y);
                if (hitTest.Type == DataGridViewHitTestType.ColumnHeader)
                {
                    DataGridViewColumn hitCol = dataGridView1.Columns[hitTest.ColumnIndex];
                    SortColumn(hitCol);
                    sortedColumn = hitCol;
                }
            }

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

        void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0 
                && dataGridView1.SelectedRows[0].Cells.Count > 0 
                && dataGridView1.SelectedRows[0].Cells[0].Value != null)
            {

                toolStripStatusLabel1.Text = "Fetching image";
                Filter newFilter = new Filter() { CardName = dataGridView1.SelectedRows[0].Cells[1].Value.ToString() };
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

        void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            needToSave = true;
        }

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            needToSave = true;
        }

        void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            toolStripStatusLabel1.Text = "Fetching image";
        }

        void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
           string msg =
      string.Format(
         "DataError occurred:\n{0}\n{1}\nDataErrorContext: {2}",
         e.Exception.GetType().ToString(), e.Exception.Message,
         e.Context);
            MessageBox.Show(msg);
        }

        private void SortColumn(DataGridViewColumn columnToSort)
        {
            ListSortDirection direction;

            // If oldColumn is null, then the DataGridView is not currently sorted.
            if (sortedColumn != null)
            {
                // Sort the same column again, reversing the SortOrder.
                if (sortedColumn == columnToSort &&
                    sortDirection == SortOrder.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    // Sort a new column and remove the old SortGlyph.
                    direction = ListSortDirection.Ascending;
                    sortedColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }

            // If no column has been selected, display an error dialog  box.
            if (columnToSort == null)
            {
                MessageBox.Show("Select a single column and try again.",
                    "Error: Invalid Selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else if (columnToSort.SortMode != DataGridViewColumnSortMode.NotSortable)
            {
                sortedColumn = columnToSort;
                sortDirection = (direction == ListSortDirection.Ascending) ? SortOrder.Ascending : SortOrder.Descending;
                dataGridView1.Sort(sortedColumn, direction);
                columnToSort.HeaderCell.SortGlyphDirection =
                    direction == ListSortDirection.Ascending ?
                    SortOrder.Ascending : SortOrder.Descending;
            }
        }

        private void ResortDataGrid()
        {
            if (sortedColumn == null) return;

            ListSortDirection direction = (sortDirection == SortOrder.Descending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
            dataGridView1.Sort(sortedColumn, direction);
        }
        #endregion

        #region Textbox Events, Filter Events
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

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (viewCardList == null)
            {
                MessageBox.Show("You must create a new view or open a view before editing!");
                return;
            }

            Rulings.GetCardRulings(new Filter() { CardName = comboBox1.Text }, AddCard_Completed);
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

        private void FilterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            filter.W = this.checkBoxW.Checked;
            filter.U = this.checkBoxU.Checked;
            filter.B = this.checkBoxB.Checked;
            filter.R = this.checkBoxR.Checked;
            filter.G = this.checkBoxG.Checked;

            FetchImage();
        }
        #endregion

        #region ToolStripMenuItem Events
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
                    OpenFile(openFileDialog1.FileName);
                    
                }
                else
                    MessageBox.Show("File not found!");
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveAt(saveFileDialog1.FileName);
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = System.Windows.Forms.DialogResult.Yes;
            if (needToSave)
            {
                res = MessageBox.Show("Do you want to save changes?", "Folio", MessageBoxButtons.YesNoCancel);
                if (res == System.Windows.Forms.DialogResult.Yes)
                {
                    if (String.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
                    {
                        if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            SaveAndUnlock(saveFileDialog1.FileName);
                        }
                    }
                    else
                    {
                        SaveAndUnlock(currentFilePath);
                    }
                }
            }

            if (res != System.Windows.Forms.DialogResult.Cancel)
            {
                viewCardList = new SortableBindingList<CardCollectionCard>();
                dataGridView1.DataSource = viewCardList;
                currentFilePath = "";
            }
        }
        #endregion

        #region Asynchronous Methods
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
        

        private void AddCard_Completed(object obj, RunWorkerCompletedEventArgs e)
        {
            IEnumerable<CardRuling> matches = (IEnumerable<CardRuling>)e.Result;
            if (matches != null && matches.Count() > 0)
            {
                viewCardList.Add((CardCollectionCard)matches.First());
            }

            comboBox1.SelectAll();
            ResortDataGrid();
        }
        #endregion

        private void SaveAt(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<CardCollectionCard>));
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    serializer.Serialize(fs, viewCardList.ToList());
                    LockDataStore(fs);
                }
            }
            else
            {
                throw new ArgumentException("The path must be valid!"); 
            }
        }

        private void OpenFile(string path)
        {
            using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.ReadWrite))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<CardCollectionCard>));
                viewCardList = new SortableBindingList<CardCollectionCard>(
                    (CardCollectionCard[])serializer.Deserialize(fs));
            }
        }

        private void SaveAndUnlock(string path)
        {
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Open, FileAccess.Write))
                {
                    XmlSerializer seralizer = new XmlSerializer(typeof(List<CardCollectionCard>));
                    seralizer.Serialize(fs, viewCardList.ToList());
                    UnlockDataStore(fs);
                }
            }
            else
            {
                throw new ArgumentException("The path must be valid!");
            }
        }

        private void LockDataStore(FileStream fs)
        {
            fs.Lock(0, fs.Length);
        }

        private void UnlockDataStore(FileStream fs)
        {
            fs.Unlock(0, fs.Length);
        }
    }
}

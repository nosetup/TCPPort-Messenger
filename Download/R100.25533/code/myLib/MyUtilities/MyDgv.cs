using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace MyUtilities
{
    internal class MyDgv
    {
        public static string TraceClass;

        public MyDgv()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }

        /// <summary> DataGridView Save text file</summary>
        public void SaveDgvToTxtFile(DataGridView dataGridView, string filePath)
        {
            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    // Write data
                    for (var i = 0; i < dataGridView.Rows.Count; i++)
                    {
                        for (var j = 0; j < dataGridView.Columns.Count; j++)
                        {
                            var cellValue = dataGridView.Rows[i].Cells[j].Value;

                            // Check if the cell contains a long string
                            if (j == dataGridView.Columns.Count - 1 && cellValue != null && cellValue.ToString().Contains('\n'))
                            {
                                sw.Write($"{cellValue}");
                            }
                            else if (cellValue != null)
                            {
                                sw.Write($"{cellValue}\t"); // Use tab as delimiter
                            }
                        }
                        sw.WriteLine(); // Move to the next line after writing a row
                    }
                }

                MessageBox.Show("Data saved to file successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        /// <summary> DataGridView Open text file</summary>
        public void OpenTxtFileToDgvDialog(DataGridView dataGridView)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var filePath = openFileDialog.FileName;

                    // logic to open and process the file content goes here


                    // Display the file content in the DataGridView
                    DisplayFileInDataGridView(dataGridView, filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        /// <summary> DataGridView Display text file to window</summary>
        public void DisplayFileInDataGridView(DataGridView dataGridView, string filePath)
        {
            try
            {
                // Read all lines from the file
                var lines = File.ReadAllLines(filePath);

                dataGridView.Rows.Clear(); // Clear existing rows

                // Populate the DataGridView with the lines from the file
                foreach (var line in lines)
                {
                    // Split the line into columns based on a delimiter (e.g., tab or comma)
                    var columns = line.Split('\t'); // using \t tabs

                    // Add a new row to the DataGridView and set the cell values
                    dataGridView.Rows.Add(columns);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary> DataGridView Delete selected row</summary>
        public void DeleteSelectedRows(DataGridView dataGridView)
        {
            // Check if any cell is selected
            if (dataGridView.SelectedCells.Count > 0)
            {
                // Create a HashSet to store unique row indices
                var uniqueRowIndices = new HashSet<int>();

                // Iterate through all selected cells and store unique row indices
                foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                {
                    uniqueRowIndices.Add(cell.RowIndex);
                }

                // Remove the entire rows at the specified indices
                foreach (var rowIndex in uniqueRowIndices.OrderByDescending(i => i))
                {
                    dataGridView.Rows.RemoveAt(rowIndex);
                }
            }
        }
        /// <summary> DataGridView AutoScroll</summary>
        public void ScrollToLastRecord(DataGridView dataGridView)
        {
            // Scroll to the last displayed row
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
        }
        /// <summary> DataGridView Used to set font color after adding a row/record</summary>
        public void SetFontColorForLastRow(DataGridView dataGridView, Color color)
        {
            // Check if there's at least one row
            if (dataGridView.Rows.Count > 0)
            {
                // Set the font color for the cells in the last row
                var lastRow = dataGridView.Rows[dataGridView.Rows.Count - 1];
                foreach (DataGridViewCell cell in lastRow.Cells)
                {
                    cell.Style.ForeColor = color;
                }
            }
        }
        /// <summary> DataGridView Search a item on a column, hide all non revelant rows</summary>
        public void SearchAndShowResults(DataGridView dataGridView, int columnIndex, string searchTerm)
        {
            // Loop through each row in the DataGridView
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                // Get the cell in the specified column
                var cell = row.Cells[columnIndex];

                // Check if the cell value contains the search term (case-insensitive)
                if (cell.Value != null && cell.Value.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Set the row Visible property to true to show the result
                    row.Visible = true;
                }
                else
                {
                    // If the cell value doesn't contain the search term, hide the row
                    row.Visible = false;
                }
            }
        }
        /// <summary> DataGridView Set all rows to Visible</summary>
        public void UnhideAllRows(DataGridView dataGridView)
        {
            // Loop through each row in the DataGridView
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                // Set the Visible property to true to unhide the row
                row.Visible = true;
            }
        }
    }
}

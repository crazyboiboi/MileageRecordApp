using System;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using System.Windows.Controls;
using System.Collections.Generic;

namespace MileageRecordApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///     
    public partial class MainWindow : Window
    {


        private ObservableCollection<Record> records = new ObservableCollection<Record>();
        private ObservableCollection<Record> displayedRecords = new ObservableCollection<Record>();
        private string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //private string fileName = "mileageRecordFile.txt";
        private bool fileModified = false;
        private Regex distanceRegex = new Regex(@"^[0-9]+$");
        private Regex locationRegex = new Regex(@"^[A-z][A-z0-9,\/\. ]+$");

        private uint totalDistanceTravelled;

        private int monthIndex = 0;
        private string year;


        public MainWindow()
        {
            InitializeComponent();
            
            //Load records from file
            loadRecords();
            mileageRecordTable.ItemsSource = records;

            populateYearComboBox();

        }



        void DataWindow_Closing(object sender, CancelEventArgs e)
        {
            //This only applies to Desktop application
            //Use a boolean to track whether there are changes in the application
            //If so, ask if the user wants to exit
            //Else do nothing
            if(fileModified)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Closing app without saving?",
                    "Data App",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    // If user doesn't want to close, cancel closure
                    e.Cancel = true;
                }
            }            
        }



    /*--------------------------- BUTTON FUNCTIONALITY ------------------------------ */

    //Add mileage record to the table 
    private void addButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add button clicked");

            if(!anyFieldsEmpty())
            {
                if (validateRecordInput())
                {
                    //Creates a new instance of the record 
                    Record record = new Record();
                    record.date = datePicker.SelectedDate.Value.Date;
                    record.day = datePicker.SelectedDate.Value.DayOfWeek.ToString();
                    record.startDistance = Convert.ToUInt32(startDistanceTextBox.Text); 
                    record.endDistance = Convert.ToUInt32(endDistanceTextBox.Text); 
                    record.totalDistance = record.endDistance - record.startDistance;
                    record.locationTravelled = locationTextBox.Text;

                    totalDistanceTravelled += record.totalDistance;

                    records.Add(record); 
                    mileageRecordTable.ItemsSource = records;

                    displayDistanceTravelled(totalDistanceTravelled);

                    //Reset all the input fields to blank/empty
                    resetInputFields("Record");
                    fileModified = true;
                } else
                {
                    displayMessageBox("Error", "Invalid input!", "Error");
                }

            } else
            {
                displayMessageBox("Error", "Input fields cannot be left blanked!", "Error");
            }
        }


        //Delete the selected record from the table
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Delete button clicked");
            int recordCount = mileageRecordTable.Items.Count-1;

            if (mileageRecordTable.SelectedItem != null)
            {
                try
                {
                    Record recordToBeDeleted = (Record)mileageRecordTable.SelectedItem;
                    int index = records.IndexOf(recordToBeDeleted);

                    totalDistanceTravelled -= recordToBeDeleted.totalDistance;

                    records.RemoveAt(index);

                    mileageRecordTable.ItemsSource = records;
                    mileageRecordTable.Items.Refresh();
                    fileModified = true;
                    resetInputFields("Record");

                    displayDistanceTravelled(totalDistanceTravelled);

                } catch (InvalidCastException)
                {
                    Console.WriteLine("Row is empty");
                }

            } else
            {
                Console.WriteLine("Failed to delete record!!");
            }
        }


        //Sorts the table and the list of records (according to date)
        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Refresh button clicked");

            //Sorts the record according to date and updates the data table
            records = sortRecords(records);
            mileageRecordTable.ItemsSource = records;

            //Refreshes year combobox items if record with new year has been added
            populateYearComboBox();

            resetInputFields("Record");

            saveRecords();

            //Debug
            printCurrentRecordList();
        }


        //End of month submission report output
        private void submitButton_Click(object sender, RoutedEventArgs e)
        {
            if(nameTextBox.Text != "" || vehicleNumberTextBox.Text != "")
            {
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.FileName = "Sample.pdf";
                sfd.DefaultExt = ".pdf";
                sfd.Filter = "PDF documents (.pdf)|*.pdf";

                bool? result = sfd.ShowDialog();

                if (result == true)
                {
                    string filename = sfd.FileName;
                    string filePath = Path.GetFullPath(sfd.FileName);
                    createPdfReport(Path.Combine(filePath, filename));
                    displayMessageBox("Information", "Report successfully created!", "");
                    resetInputFields("PDF");
                }
                
            } else
            {
                displayMessageBox("Error", "Input fields cannot be left blanked!", "Error");
            }
        }


        //Change datagrid content depending on the combobox month item selected
        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            monthIndex = monthComboBox.SelectedIndex;

            filterData();
        }
        //Change datagrid content depending on the combobox year item selected
        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedComboItem = sender as ComboBox;
            year = selectedComboItem.SelectedItem.ToString();
            filterData();
        }

        //Filters records depending on items selected in month and year combobox
        private void filterData ()
        {
            displayedRecords.Clear();
            ObservableCollection<Record> filteredRecords = new ObservableCollection<Record>();
            uint distanceTravelled = 0;


            if (year != null)
            {
                if (year.Equals("All") && monthIndex == 0)
                {
                    displayedRecords = records;
                } else if (year.Equals("All"))
                {
                    distanceTravelled = 0;
                    foreach (Record r in records)
                    {
                        if (r.date.Month == monthIndex)
                        {
                            displayedRecords.Add(r);
                            distanceTravelled += r.totalDistance;
                        }
                    }
                } else if (monthIndex == 0)
                {
                    distanceTravelled = 0;
                    foreach (Record r in records)
                    {
                        if (r.date.Year.ToString() == year)
                        {
                            displayedRecords.Add(r);
                            distanceTravelled += r.totalDistance;
                        }
                    }
                }
                else
                {
                    distanceTravelled = 0;
                    foreach (Record r in records)
                    {
                        if (r.date.Year.ToString() == year && r.date.Month == monthIndex)
                        {
                            displayedRecords.Add(r);
                            distanceTravelled += r.totalDistance;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("YearToFilter is null!!");
            }


            displayDistanceTravelled(distanceTravelled);
            displayedRecords = sortRecords(displayedRecords);
            mileageRecordTable.ItemsSource = displayedRecords;
            mileageRecordTable.Items.Refresh();
        }









        private void populateYearComboBox()
        {
            int minYear = records[0].date.Year;
            int maxYear = records[records.Count-1].date.Year;

            int steps = maxYear - minYear;

            List<string> years = new List<string>();

            years.Add("All");

            for (int i=0; i<=steps; i++)
            {
                int yearToAdd = minYear + i;
                years.Add(yearToAdd.ToString());
                //yearComboBox.Items.Add(minYear + i);
            }

            yearComboBox.ItemsSource = years;
            yearComboBox.Items.Refresh();

        }







        //Message Box
        private void displayMessageBox (string type, string text, string title)
        {
            if(type.Equals("Error"))
            {
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
            } else if(type.Equals("Information"))
            {
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        //Display distance travelled
        private void displayDistanceTravelled (uint distanceTravelled)
        {
            distanceTravelledTextBox.Text = "Total Distance Travelled (km): " + distanceTravelled;
        }


        //Resets all the input fields after adding the record
        private void resetInputFields(string type)
        {
            if(type.Equals("Record"))
            {
                datePicker.Text = "";
                startDistanceTextBox.Text = "";
                endDistanceTextBox.Text = "";
                locationTextBox.Text = "";
                monthComboBox.SelectedIndex = 0;
            } else if (type.Equals("PDF"))
            {
                nameTextBox.Text = "";
                vehicleNumberTextBox.Text = "";
            }
            
        }

        
        //Check if any one of the input fields are empty
        private bool anyFieldsEmpty()
        {
            return datePicker.Text == "" || 
                startDistanceTextBox.Text == "" ||
                endDistanceTextBox.Text == "" ||
                locationTextBox.Text == "";
        }


        //Validate input fields for distance and location inputs
        private bool validateRecordInput()
        {
            return distanceRegex.IsMatch(startDistanceTextBox.Text) &&
                distanceRegex.IsMatch(endDistanceTextBox.Text) &&
                locationRegex.IsMatch(locationTextBox.Text);
        }


        //Sort the record
        private ObservableCollection<Record> sortRecords(ObservableCollection<Record> recordsToBeSorted)
        {
            return new ObservableCollection<Record>(recordsToBeSorted.OrderBy(r => r.date));
        }


        //Save all records to textfile
        private void saveRecords()
        {
            fileModified = false;

            using (StreamWriter sw = new StreamWriter(Path.Combine(desktopPath, "Sample.txt")))
            {
                sw.WriteLine(totalDistanceTravelled);

                foreach (Record r in records)
                {
                    sw.Write(r.date + ",");
                    sw.Write(r.day + ",");
                    sw.Write(r.startDistance + ",");
                    sw.Write(r.endDistance + ",");
                    sw.Write(r.totalDistance + ",");
                    sw.Write(r.locationTravelled + ",");
                    sw.WriteLine(r.remark);
                }
            }
            displayMessageBox("Information", "Saved successfully", "");
        }



        //Read all records from textfile and populate the list
        private void loadRecords ()
        {
            try
            {
                string[] lines = File.ReadAllLines(Path.Combine(desktopPath, "Sample.txt"));

                totalDistanceTravelled = Convert.ToUInt32(lines[0]);
                displayDistanceTravelled(totalDistanceTravelled);

                for (int i=1; i<lines.Length; i++)
                {
                    string[] col = lines[i].Split(new char[] { ',' });
                    Record record = new Record
                    {
                        date = Convert.ToDateTime(col[0]),
                        day = col[1],
                        startDistance = Convert.ToUInt32(col[2]),
                        endDistance = Convert.ToUInt32(col[3]),
                        totalDistance = Convert.ToUInt32(col[4]),
                        locationTravelled = col[5],
                        remark = col[6]
                    };

                    records.Add(record);
                }
            } catch (FileNotFoundException e)
            {
                Console.WriteLine("Sample.txt is not found, loading the app in default state");
                displayMessageBox("Error", "Failed to locate and read file!", "Error");
            }            
        }


        //Debug: print out the current state of the record list
        void printCurrentRecordList()
        {
            Console.WriteLine("Total number of records: " + records.Count());
            foreach(Record r in records)
            {
                Console.WriteLine(r.ToString());
            }
        }


        //Creates the PDF report with the format
        void createPdfReport (string path)
        {
            using (var writer = new PdfWriter(path))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    var doc = new Document(pdf, PageSize.A4);
                    //Header information
                    doc.Add(createParagraphWithTab("Name ", nameTextBox.Text));
                    doc.Add(createParagraphWithTab("Month ", monthComboBox.Text));
                    doc.Add(createParagraphWithTab("Vehicle Number ", vehicleNumberTextBox.Text));

                    doc.Add(new Paragraph());

                    //Table information
                    float[] pointColumnWidths = { 75f, 75f, 112.5f, 112.5f, 112.5f, 150f };
                    Table table = new Table(pointColumnWidths);

                    table.AddCell(new Cell().Add(new Paragraph("Date")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddCell(new Cell().Add(new Paragraph("Day")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddCell(new Cell().Add(new Paragraph("Start (km)")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddCell(new Cell().Add(new Paragraph("End (km)")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddCell(new Cell().Add(new Paragraph("Total (km)")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddCell(new Cell().Add(new Paragraph("Location Travelled")).SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    //Read all records from the list
                    foreach (Record r in displayedRecords)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(r.date.ToShortDateString())));
                        table.AddCell(new Cell().Add(new Paragraph(r.day)));
                        table.AddCell(new Cell().Add(new Paragraph(r.startDistance.ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(r.endDistance.ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(r.totalDistance.ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(r.locationTravelled)));
                    }
                    Console.WriteLine("I can run");


                    doc.Add(table);
                }
            }
        }


        private Paragraph createParagraphWithTab (string key, string value)
        {
            Paragraph p = new Paragraph();
            p.AddTabStops(new TabStop(100f, TabAlignment.LEFT));
            p.Add(key);
            p.Add(new Tab());
            p.Add(new Paragraph(": " + value));

            return p;
        }

        
    }
}

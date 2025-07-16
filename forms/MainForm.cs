using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RakhesApp.Models;

namespace RakhesApp.Forms
{
    public partial class MainForm : Form
    {
        private StatisticsManager _statisticsManager;
        private Label _currentDateLabel;
        private GroupBox _addLicenseGroup;
        private GroupBox _statisticsGroup;
        private ComboBox _licenseTypeCombo;
        private TextBox _descriptionText;
        private TextBox _referenceNumberText;
        private Button _addButton;
        private Button _cancelButton;
        private DataGridView _recordsGrid;
        private Label[] _quarterlyLabels;
        private Label[] _yearlyLabels;

        public MainForm()
        {
            _statisticsManager = new StatisticsManager();
            InitializeComponent();
            SetupArabicInterface();
            UpdateStatistics();
        }

        private void InitializeComponent()
        {
            this.Text = "تطبيق إدارة الرخص";
            this.Size = new Size(1000, 700);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Tahoma", 10F, FontStyle.Regular);

            // تاريخ اليوم
            _currentDateLabel = new Label
            {
                Text = $"التاريخ الحالي: {DateTime.Now.ToString("yyyy/MM/dd")}",
                Location = new Point(20, 20),
                Size = new Size(300, 30),
                Font = new Font("Tahoma", 12F, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // مجموعة إضافة الرخص
            _addLicenseGroup = new GroupBox
            {
                Text = "إضافة رخصة جديدة",
                Location = new Point(20, 60),
                Size = new Size(450, 200),
                Font = new Font("Tahoma", 10F, FontStyle.Bold)
            };

            // نوع الرخصة
            var typeLabel = new Label
            {
                Text = "نوع الرخصة:",
                Location = new Point(20, 30),
                Size = new Size(100, 25)
            };

            _licenseTypeCombo = new ComboBox
            {
                Location = new Point(130, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _licenseTypeCombo.Items.AddRange(new string[] 
            {
                "تصريح",
                "الرخص", 
                "قرارات التحويل",
                "قرارات الالغاء"
            });

            // رقم المرجع
            var refLabel = new Label
            {
                Text = "رقم المرجع:",
                Location = new Point(20, 70),
                Size = new Size(100, 25)
            };

            _referenceNumberText = new TextBox
            {
                Location = new Point(130, 70),
                Size = new Size(200, 25)
            };

            // الوصف
            var descLabel = new Label
            {
                Text = "الوصف:",
                Location = new Point(20, 110),
                Size = new Size(100, 25)
            };

            _descriptionText = new TextBox
            {
                Location = new Point(130, 110),
                Size = new Size(200, 25)
            };

            // أزرار
            _addButton = new Button
            {
                Text = "إضافة",
                Location = new Point(130, 150),
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };
            _addButton.Click += AddButton_Click;

            _cancelButton = new Button
            {
                Text = "إلغاء المحدد",
                Location = new Point(220, 150),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral
            };
            _cancelButton.Click += CancelButton_Click;

            // إضافة العناصر للمجموعة
            _addLicenseGroup.Controls.AddRange(new Control[] 
            {
                typeLabel, _licenseTypeCombo, refLabel, _referenceNumberText,
                descLabel, _descriptionText, _addButton, _cancelButton
            });

            // مجموعة الإحصائيات
            _statisticsGroup = new GroupBox
            {
                Text = "الإحصائيات",
                Location = new Point(500, 60),
                Size = new Size(450, 200),
                Font = new Font("Tahoma", 10F, FontStyle.Bold)
            };

            SetupStatisticsLabels();

            // جدول السجلات
            _recordsGrid = new DataGridView
            {
                Location = new Point(20, 280),
                Size = new Size(930, 350),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            SetupDataGrid();

            // إضافة العناصر للنموذج
            this.Controls.AddRange(new Control[] 
            {
                _currentDateLabel, _addLicenseGroup, _statisticsGroup, _recordsGrid
            });
        }

        private void SetupStatisticsLabels()
        {
            var quarterlyTitle = new Label
            {
                Text = "إحصائيات الربع الحالي (7-8-9):",
                Location = new Point(20, 30),
                Size = new Size(200, 25),
                Font = new Font("Tahoma", 9F, FontStyle.Bold)
            };

            var yearlyTitle = new Label
            {
                Text = "إحصائيات السنة الحالية:",
                Location = new Point(250, 30),
                Size = new Size(180, 25),
                Font = new Font("Tahoma", 9F, FontStyle.Bold)
            };

            _quarterlyLabels = new Label[4];
            _yearlyLabels = new Label[4];

            string[] typeNames = { "تصريح", "الرخص", "قرارات التحويل", "قرارات الالغاء" };

            for (int i = 0; i < 4; i++)
            {
                _quarterlyLabels[i] = new Label
                {
                    Text = $"{typeNames[i]}: 0",
                    Location = new Point(20, 60 + (i * 25)),
                    Size = new Size(200, 20)
                };

                _yearlyLabels[i] = new Label
                {
                    Text = $"{typeNames[i]}: 0",
                    Location = new Point(250, 60 + (i * 25)),
                    Size = new Size(180, 20)
                };
            }

            _statisticsGroup.Controls.Add(quarterlyTitle);
            _statisticsGroup.Controls.Add(yearlyTitle);
            _statisticsGroup.Controls.AddRange(_quarterlyLabels);
            _statisticsGroup.Controls.AddRange(_yearlyLabels);
        }

        private void SetupDataGrid()
        {
            _recordsGrid.Columns.Add("Id", "الرقم");
            _recordsGrid.Columns.Add("Type", "النوع");
            _recordsGrid.Columns.Add("ReferenceNumber", "رقم المرجع");
            _recordsGrid.Columns.Add("Description", "الوصف");
            _recordsGrid.Columns.Add("DateAdded", "تاريخ الإضافة");
            _recordsGrid.Columns.Add("IsActive", "الحالة");

            _recordsGrid.Columns["Id"].Width = 60;
            _recordsGrid.Columns["Type"].Width = 120;
            _recordsGrid.Columns["ReferenceNumber"].Width = 120;
            _recordsGrid.Columns["Description"].Width = 200;
            _recordsGrid.Columns["DateAdded"].Width = 120;
            _recordsGrid.Columns["IsActive"].Width = 80;
        }

        private void SetupArabicInterface()
        {
            // تحديث التاريخ كل ثانية
            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => 
            {
                _currentDateLabel.Text = $"التاريخ الحالي: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}";
            };
            timer.Start();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (_licenseTypeCombo.SelectedIndex == -1 || 
                string.IsNullOrWhiteSpace(_referenceNumberText.Text))
            {
                MessageBox.Show("يرجى ملء جميع الحقول المطلوبة", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var record = new LicenseRecord
            {
                Type = (LicenseType)(_licenseTypeCombo.SelectedIndex + 1),
                ReferenceNumber = _referenceNumberText.Text,
                Description = _descriptionText.Text
            };

            _statisticsManager.AddRecord(record);
            
            MessageBox.Show("تم إضافة الرخصة بنجاح", "نجح", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            ClearForm();
            UpdateStatistics();
            RefreshDataGrid();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (_recordsGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى تحديد سجل لإلغائه", "خطأ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = _recordsGrid.SelectedRows[0];
            var recordId = (int)selectedRow.Cells["Id"].Value;

            var result = MessageBox.Show("هل أنت متأكد من إلغاء هذا السجل؟", "تأكيد", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _statisticsManager.CancelRecord(recordId);
                MessageBox.Show("تم إلغاء السجل بنجاح", "نجح", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                UpdateStatistics();
                RefreshDataGrid();
            }
        }

        private void UpdateStatistics()
        {
            var quarterlyStats = _statisticsManager.GetQuarterlyStatistics();
            var yearlyStats = _statisticsManager.GetYearlyStatistics();

            string[] typeNames = { "تصريح", "الرخص", "قرارات التحويل", "قرارات الالغاء" };

            for (int i = 0; i < 4; i++)
            {
                var type = (LicenseType)(i + 1);
                _quarterlyLabels[i].Text = $"{typeNames[i]}: {quarterlyStats[type]}";
                _yearlyLabels[i].Text = $"{typeNames[i]}: {yearlyStats[type]}";
            }
        }

        private void RefreshDataGrid()
        {
            _recordsGrid.Rows.Clear();
            var records = _statisticsManager.GetAllRecords();

            foreach (var record in records)
            {
                string typeName = "";
                switch (record.Type)
                {
                    case LicenseType.تصريح: typeName = "تصريح"; break;
                    case LicenseType.الرخص: typeName = "الرخص"; break;
                    case LicenseType.قرارات_التحويل: typeName = "قرارات التحويل"; break;
                    case LicenseType.قرارات_الالغاء: typeName = "قرارات الالغاء"; break;
                }

                _recordsGrid.Rows.Add(
                    record.Id,
                    typeName,
                    record.ReferenceNumber,
                    record.Description,
                    record.DateAdded.ToString("yyyy/MM/dd"),
                    record.IsActive ? "نشط" : "ملغى"
                );
            }
        }

        private void ClearForm()
        {
            _licenseTypeCombo.SelectedIndex = -1;
            _referenceNumberText.Clear();
            _descriptionText.Clear();
        }
    }
}

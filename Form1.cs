using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Reflection;


namespace LicenseManagement
{
    public partial class Form1 : Form
    {
        private DatabaseManager dbManager;
        private System.Windows.Forms.Timer dateTimer;

        // Contrôles UI
        private ComboBox cmbType;
        private TextBox txtReference;
        private Button btnAdd;
        private Button btnmodify;
        private Button btnCancel;
        private Button btnDelete;
        private Label lblCurrentDate;
        private Label lblQuarterlyStats;
        private Label lblYearlyStats;
        private DataGridView dgvLicenses;
        private GroupBox gbAdd;
        private ComboBox cmbActivites;
        private GroupBox gbStats;
        private GroupBox gbList;
        private GroupBox gbModifier;
        private ComboBox cmbType_Activite;
        private Label lbl_Activite;
        private Label lbl_personne;
        private ComboBox cmb_personne;
        private MenuStrip menuStrip;
        private ToolStripDropDownButton menuButton;
        private DataGridView dgvActivities; // Nouveau DataGridView pour les statistiques d'activités
        private bool isShowingLicenses = true;
        private GroupBox gbSearch;
        private TextBox txtSearch;
        private ComboBox cmbSearchFilter;
        private ComboBox cmbSifaFilter;
        private Button btnClearSearch;
        private List<License> allLicensesCache;
        private void CleanupEventHandlers()
        {
            // Nettoyer les événements du DataGridView
            dgvLicenses.CellDoubleClick -= DgvLicenses_CellDoubleClick;
            dgvLicenses.SelectionChanged -= DgvLicenses_SelectionChanged;
            dgvLicenses.CellFormatting -= DgvLicenses_CellFormatting;

            // Nettoyer les événements des boutons
            btnAdd.Click -= BtnAdd_Click;
            btnCancel.Click -= BtnCancel_Click;
            btnmodify.Click -= btnModifier_Click;
            btnDelete.Click -= BtnDelete_Click;
            cmbType.SelectedIndexChanged -= CmbType_SelectedIndexChanged;
        }
        public Form1()
        {
            InitializeComponent();
            InitializeFormSettings();
            InitializeDatabase();
            SetupUI();
            LoadData();

            // ✅ IMPORTANT : Appeler UpdateStatistics APRÈS la création de l'UI
            UpdateStatistics();

            // ✅ AJOUT : Activer explicitement tous les boutons au démarrage
            btnAdd.Enabled = true;
            btnmodify.Enabled = true;
            btnDelete.Enabled = true;
            btnCancel.Enabled = true;

            // Timer pour mettre à jour la date
            dateTimer = new System.Windows.Forms.Timer();
            dateTimer.Interval = 1000; // 1 seconde
            dateTimer.Tick += DateTimer_Tick;
            dateTimer.Start();
        }
        private void InitializeFormSettings()
        {
            this.SuspendLayout();
            this.Text = "رخص - نظام إدارة التراخيص";
            this.WindowState = FormWindowState.Maximized; // ✅ AJOUT: Fenêtre maximisée
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Arial", 10F, FontStyle.Regular);

            this.ResumeLayout(false);
        }

        // 5. Modifier btnModifier_Click pour gérer طلب الإلغاء avec dialogue en arabe
        private void btnModifier_Click(object sender, EventArgs e)
        {
            if (btnmodify.Text == "تعديل")
            {
                // Mode: Select for editing
                if (dgvLicenses.SelectedRows.Count == 0)
                {
                    MessageBox.Show("يرجى اختيار ترخيص للتعديل", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedRow = dgvLicenses.SelectedRows[0];
                var reference = selectedRow.Cells["Reference"].Value.ToString();
                var type = selectedRow.Cells["Type"].Value.ToString();
                var sifa = selectedRow.Cells["Sifa"].Value?.ToString() ?? "شخص ذاتي";
                var nom = selectedRow.Cells["Nom"].Value?.ToString() ?? "";

                // Fill form with selected data
                txtReference.Text = reference;
                cmbType.SelectedItem = type;
                cmb_personne.SelectedItem = sifa;
                Control txtNom = gbAdd.Controls["txtNom"];
                txtNom.Text = nom;

                // Store original reference for update
                txtReference.Tag = reference;

                // Change button text to save mode
                btnmodify.Text = "حفظ التعديل";
                btnmodify.BackColor = Color.Orange;

                // Enable/disable other buttons
                btnAdd.Enabled = false;
                btnDelete.Enabled = false;
            }
            else
            {
                // Mode: Save changes - gérer طلب الإلغاء
                string selectedType = cmbType.SelectedItem?.ToString();
                Control txtNom = gbAdd.Controls["txtNom"];

                if (selectedType == "طلب الإلغاء")
                {
                    if (string.IsNullOrWhiteSpace(txtReference.Text))
                    {
                        MessageBox.Show("يرجى إدخال المرجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string originalReference = txtReference.Tag?.ToString();

                    // Si la référence a changé, vérifier si elle existe
                    if (txtReference.Text.Trim() != originalReference && dbManager.ReferenceExistsAndActive(txtReference.Text.Trim()))
                    {
                        var result = MessageBox.Show(
                            "هذا المرجع موجود بالفعل في النظام.\nهل تريد إلغاء هذا الترخيص؟",
                            "ترخيص موجود",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            if (dbManager.CancelExistingLicense(txtReference.Text.Trim()))
                            {
                                MessageBox.Show("تم إلغاء الترخيص بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // Supprimer l'ancien enregistrement
                                dbManager.DeleteLicense(originalReference);
                                ClearForm();
                                LoadData();
                                UpdateStatistics();
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    // NOUVEAU CAS: Si la référence n'existe pas, permettre la modification
                    else if (txtReference.Text.Trim() != originalReference && !dbManager.ReferenceExistsAndActive(txtReference.Text.Trim()))
                    {
                        // Permettre la modification - la référence sera mise à jour comme طلب الإلغاء
                        // Le code continuera en bas pour effectuer la mise à jour normale
                    }
                }
                else if (selectedType == "طلب تحويل")
                {
                    if (string.IsNullOrWhiteSpace(txtNom.Text))
                    {
                        MessageBox.Show("يرجى إدخال الاسم", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                else
                {
                    if (cmbType.SelectedItem == null || string.IsNullOrWhiteSpace(txtReference.Text) ||
                        string.IsNullOrWhiteSpace(txtNom.Text) || cmb_personne.SelectedItem == null)
                    {
                        MessageBox.Show("يرجى ملء جميع الحقول", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string originalReference = txtReference.Tag?.ToString();
                    if (string.IsNullOrEmpty(originalReference))
                    {
                        MessageBox.Show("خطأ في تحديد الترخيص الأصلي", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Check for duplicate reference (excluding current one)
                    if (dbManager.ReferenceExists(txtReference.Text.Trim(), originalReference))
                    {
                        MessageBox.Show("هذا المرجع موجود بالفعل، يرجى استخدام مرجع آخر", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string originalRef = txtReference.Tag?.ToString();
                string status = selectedType == "قرار الإلغاء" || selectedType == "طلب الإلغاء" ? "ملغي" : "نشط";
                string subType = "-";
                string sifa = cmb_personne.SelectedItem?.ToString() ?? "شخص ذاتي";
                string nom = txtNom.Text?.Trim() ?? "";

                if (selectedType == "رخصة")
                {
                    subType = CustomMessageBox.Show(
                        "اختر نوع الرخصة",
                        "يرجى اختيار نوع الرخصة:",
                        "دفتر التحملات",
                        "بحت المنافع و الاضرار"
                    );
                }

                var license = new License
                {
                    Type = selectedType,
                    Reference = txtReference.Text.Trim(),
                    Date = DateTime.Now,
                    Status = status,
                    SubType = subType,
                    Sifa = sifa,
                    Nom = nom
                };

                // MODIFICATION: Si c'est طلب الإلغاء, mettre à jour DateAnnulation
                if (selectedType == "طلب الإلغاء")
                {
                    license.DateAnnulation = DateTime.Now;
                }

                if (dbManager.UpdateLicense(license, originalRef))
                {
                    MessageBox.Show("تم تحديث الترخيص بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    LoadData();
                    UpdateStatistics();

                    // Reset button state
                    btnmodify.Text = "تعديل";
                    btnmodify.BackColor = Color.BlueViolet;
                    btnAdd.Enabled = true;
                    btnDelete.Enabled = true;
                }
                else
                {
                    MessageBox.Show("فشل في تحديث الترخيص", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void InitializeDatabase()
        {
            dbManager = new DatabaseManager();
            dbManager.InitializeDatabase();
        }

        private void SetupUI()
        {

            CreateTopMenu();
            SetupAddGroup();
            SetupSearchGroup();
            // Groupe d'ajout
            gbAdd = new GroupBox();
            gbAdd.Text = "إضافة جديد";
            gbAdd.Location = new Point(20, 50);
            gbAdd.Size = new Size(this.ClientSize.Width - 40, 180); // ✅ AUGMENTÉ pour plus d'espace
            gbAdd.RightToLeft = RightToLeft.Yes;
            //gbAdd.RightToLeftLayout = true; // ✅ AJOUTÉ
            gbAdd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbAdd.Font = new Font("Arial", 10F, FontStyle.Bold);

            // Label الأنشطة
            Label lblActivites = new Label();
            lblActivites.Text = "الأنشطة:";
            lblActivites.Location = new Point(850, 65);
            lblActivites.Size = new Size(80, 23);
            lblActivites.TextAlign = ContentAlignment.MiddleRight;

            cmbType_Activite = new ComboBox();
            cmbType_Activite.Name = "cmbType_Activite";
            cmbType_Activite.Location = new Point(270, 65);
            cmbType_Activite.Size = new Size(580, 23);
            cmbType_Activite.DropDownStyle = ComboBoxStyle.DropDown;
            cmbType_Activite.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbType_Activite.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbType_Activite.RightToLeft = RightToLeft.Yes;

            // Type de licence
            Label lblType = new Label();
            lblType.Text = "النوع:";
            lblType.Location = new Point(850, 30);
            lblType.Size = new Size(80, 23);
            lblType.TextAlign = ContentAlignment.MiddleRight;

            cmbType = new ComboBox();
            cmbType.Items.Clear(); // S'assurer que la liste est vide
            cmbType.Items.AddRange(new string[] {
    "تصريح",
    "رخصة",
    "قرار تحويل",    // ✅ MODIFIÉ: "طلب تحويل" → "قرار تحويل"
    "طلب الإلغاء"
});
            cmbType.Location = new Point(650, 30);
            cmbType.Size = new Size(190, 23);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.RightToLeft = RightToLeft.Yes;
            cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;

            Label lblNom = new Label();
            lblNom.Text = "الاسم:";
            lblNom.Location = new Point(350, 20);
            lblNom.Size = new Size(40, 23);
            lblNom.TextAlign = ContentAlignment.MiddleRight;

            TextBox txtNom = new TextBox();
            txtNom.Name = "txtNom";
            txtNom.Location = new Point(150, 30);
            txtNom.Size = new Size(170, 23);
            txtNom.RightToLeft = RightToLeft.Yes;

            // Référence
            Label lblReference = new Label();
            lblReference.Text = "المرجع:";
            lblReference.Location = new Point(590, 30);
            lblReference.Size = new Size(50, 23);
            lblReference.TextAlign = ContentAlignment.MiddleRight;

            txtReference = new TextBox();
            txtReference.Location = new Point(400, 30);
            txtReference.Size = new Size(180, 23);
            txtReference.RightToLeft = RightToLeft.Yes;

            // Label الصفة
            Label lblSifa = new Label();
            lblSifa.Text = "الصفة:";
            lblSifa.Location = new Point(850, 93);
            lblSifa.Size = new Size(50, 23);
            lblSifa.TextAlign = ContentAlignment.MiddleRight;

            // ComboBox الصفة
            cmb_personne = new ComboBox();
            cmb_personne.Name = "cmb_personne";
            cmb_personne.Location = new Point(700, 91);
            cmb_personne.Size = new Size(150, 23);
            cmb_personne.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_personne.Items.AddRange(new string[] { "شخص ذاتي", "شخص معنوي" });
            cmb_personne.RightToLeft = RightToLeft.Yes;

            // Date actuelle
            lblCurrentDate = new Label();
            lblCurrentDate.Location = new Point(400, 93);
            lblCurrentDate.Size = new Size(245, 23);
            lblCurrentDate.Font = new Font("Arial", 12F, FontStyle.Bold);
            lblCurrentDate.ForeColor = Color.Blue;

            // Boutons - CORRIGÉ l'emplacement
            btnAdd = new Button();
            btnAdd.Text = "إضافة";
            btnAdd.Location = new Point(300, 120);
            btnAdd.Size = new Size(80, 30);
            btnAdd.BackColor = Color.LightGreen;
            btnAdd.Click += BtnAdd_Click;

            btnCancel = new Button();
            btnCancel.Text = "مسح";
            btnCancel.Location = new Point(210, 120); // Corrigé - était sur la même position que btnAdd
            btnCancel.Size = new Size(80, 30);
            btnCancel.BackColor = Color.LightCoral;
            btnCancel.Click += BtnCancel_Click;

            btnmodify = new Button();
            btnmodify.Text = "تعديل";
            btnmodify.Location = new Point(120, 120); // Corrigé
            btnmodify.Size = new Size(85, 30);
            btnmodify.BackColor = Color.BlueViolet;
            btnmodify.Click += btnModifier_Click;
            btnmodify.ForeColor = Color.White;

            btnDelete = new Button();
            btnDelete.Text = "حذف";
            btnDelete.Location = new Point(30, 120); // Corrigé
            btnDelete.Size = new Size(80, 30);
            btnDelete.BackColor = Color.Red;
            btnDelete.ForeColor = Color.White;
            btnDelete.Click += BtnDelete_Click;

            gbAdd.Controls.AddRange(new Control[] {
        lblType, cmbType,
        lblReference, txtReference,
        btnAdd, btnCancel, btnDelete, btnmodify,
        lblNom, txtNom,
        lblCurrentDate,
        lblActivites, cmbType_Activite,
        lblSifa, cmb_personne
    });


            SetupAddGroupControls();

            // Groupe des statistiques - AJUSTÉ
            gbStats = new GroupBox();
            gbStats.Text = "الإحصائيات";
            gbStats.Location = new Point(20, 300);
            gbStats.Size = new Size(this.ClientSize.Width - 40, 100);
            gbStats.RightToLeft = RightToLeft.Yes;
            gbStats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbStats.Font = new Font("Arial", 10F, FontStyle.Bold);
            gbStats.ForeColor = Color.Black;
            gbStats.BackColor = Color.FromArgb(245, 245, 245);

            lblQuarterlyStats = new Label();
            lblQuarterlyStats.Location = new Point(720, 25);
            lblQuarterlyStats.Size = new Size(gbStats.Width - 40, 25);
            lblQuarterlyStats.Font = new Font("Arial", 10F, FontStyle.Bold);
            lblQuarterlyStats.ForeColor = Color.DarkBlue;
            lblQuarterlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ ALIGNEMENT À DROITE
            lblQuarterlyStats.RightToLeft = RightToLeft.Yes;

            lblQuarterlyStats.Text = "جارٍ تحميل الإحصائيات...";

            lblYearlyStats = new Label();
            lblYearlyStats.Location = new Point(785, 55);
            lblYearlyStats.Size = new Size(gbStats.Width - 40, 25);
            lblYearlyStats.Font = new Font("Arial", 10F, FontStyle.Bold);
            lblYearlyStats.ForeColor = Color.DarkGreen;
            lblYearlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ ALIGNEMENT À DROITE
            lblYearlyStats.RightToLeft = RightToLeft.Yes;
            lblYearlyStats.Text = "جارٍ تحميل الإحصائيات...";

            gbStats.Controls.AddRange(new Control[] { lblQuarterlyStats, lblYearlyStats });

            dgvLicenses = new DataGridView();
            dgvLicenses.Dock = DockStyle.Fill;
            dgvLicenses.AllowUserToAddRows = false;
            dgvLicenses.AllowUserToDeleteRows = false;
            dgvLicenses.ReadOnly = true;
            dgvLicenses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLicenses.MultiSelect = false;
            dgvLicenses.RightToLeft = RightToLeft.Yes;

            // Configuration visuelle améliorée

            gbList = new GroupBox();
            gbList.Text = "قائمة التراخيص";
            gbList.Location = new Point(20, 410);
            gbList.Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 450);
            gbList.RightToLeft = RightToLeft.Yes;
            gbList.Font = new Font("Arial", 10F, FontStyle.Bold);
            gbList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            SetupDataGridViews();
            gbList.Controls.Add(dgvLicenses);

            // Ajouter tous les groupes au formulaire
            this.Controls.AddRange(new Control[] { menuStrip, gbAdd, gbSearch, gbStats, gbList });
        }
        private void SetupAddGroup()
        {
            // Groupe d'ajout
            gbAdd = new GroupBox();
            gbAdd.Text = "إضافة جديد";
            gbAdd.Location = new Point(20, 50);
            gbAdd.Size = new Size(this.ClientSize.Width - 40, 180);
            gbAdd.RightToLeft = RightToLeft.Yes;
            gbAdd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbAdd.Font = new Font("Arial", 10F, FontStyle.Bold);

            SetupAddGroupControls();
        }
        private void SetupSearchGroup()
        {
            gbSearch = new GroupBox();
            gbSearch.Text = "البحث والتصفية";
            gbSearch.Location = new Point(20, 240); // Entre gbAdd et gbStats
            gbSearch.Size = new Size(this.ClientSize.Width - 40, 60);
            gbSearch.RightToLeft = RightToLeft.Yes;
            gbSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbSearch.Font = new Font("Arial", 10F, FontStyle.Bold);

            int formWidth = gbSearch.Width;
            int margin = 20;
            int controlHeight = 25;
            int labelWidth = 80;
            int comboWidth = 150;
            int textBoxWidth = 200;
            int buttonWidth = 80;

            // Label البحث
            Label lblSearch = new Label();
            lblSearch.Text = "البحث:";
            lblSearch.Location = new Point(formWidth - margin - labelWidth, 25);
            lblSearch.Size = new Size(labelWidth, controlHeight);
            lblSearch.TextAlign = ContentAlignment.MiddleRight;
            lblSearch.Font = new Font("Arial", 10F);

            // TextBox البحث
            txtSearch = new TextBox();
            txtSearch.Location = new Point(formWidth - margin - labelWidth - textBoxWidth - 10, 25);
            txtSearch.Size = new Size(textBoxWidth, controlHeight);
            txtSearch.RightToLeft = RightToLeft.Yes;
            txtSearch.Font = new Font("Arial", 10F);
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // Label البحث في
            Label lblSearchFilter = new Label();
            lblSearchFilter.Text = "البحث في:";
            lblSearchFilter.Location = new Point(formWidth - margin - labelWidth - textBoxWidth - 20 - labelWidth, 25);
            lblSearchFilter.Size = new Size(labelWidth, controlHeight);
            lblSearchFilter.TextAlign = ContentAlignment.MiddleRight;
            lblSearchFilter.Font = new Font("Arial", 10F);

            // ComboBox نوع البحث
            cmbSearchFilter = new ComboBox();
            cmbSearchFilter.Location = new Point(formWidth - margin - labelWidth - textBoxWidth - 20 - labelWidth - comboWidth - 10, 25);
            cmbSearchFilter.Size = new Size(comboWidth, controlHeight);
            cmbSearchFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSearchFilter.Items.AddRange(new string[] {
        "الكل",
        "المرجع",
        "الاسم",
        "النشاط",
        "التاريخ",
        "النوع"
    });
            cmbSearchFilter.SelectedIndex = 0; // الكل par défaut
            cmbSearchFilter.RightToLeft = RightToLeft.Yes;
            cmbSearchFilter.Font = new Font("Arial", 10F);
            cmbSearchFilter.SelectedIndexChanged += CmbSearchFilter_SelectedIndexChanged;

            // Label تصفية الصفة
            Label lblSifaFilter = new Label();
            lblSifaFilter.Text = "الصفة:";
            lblSifaFilter.Location = new Point(formWidth - margin - labelWidth - textBoxWidth - 20 - labelWidth - comboWidth - 20 - labelWidth, 25);
            lblSifaFilter.Size = new Size(labelWidth, controlHeight);
            lblSifaFilter.TextAlign = ContentAlignment.MiddleRight;
            lblSifaFilter.Font = new Font("Arial", 10F);

            // ComboBox تصفية الصفة
            cmbSifaFilter = new ComboBox();
            cmbSifaFilter.Location = new Point(formWidth - margin - labelWidth - textBoxWidth - 20 - labelWidth - comboWidth - 20 - labelWidth - comboWidth - 10, 25);
            cmbSifaFilter.Size = new Size(comboWidth, controlHeight);
            cmbSifaFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSifaFilter.Items.AddRange(new string[] {
        "الكل",
        "شخص ذاتي",
        "شخص معنوي"
    });
            cmbSifaFilter.SelectedIndex = 0; // الكل par défaut
            cmbSifaFilter.RightToLeft = RightToLeft.Yes;
            cmbSifaFilter.Font = new Font("Arial", 10F);
            cmbSifaFilter.SelectedIndexChanged += CmbSifaFilter_SelectedIndexChanged;

            // Bouton مسح البحث
            btnClearSearch = new Button();
            btnClearSearch.Text = "مسح";
            btnClearSearch.Location = new Point(40, 23);
            btnClearSearch.Size = new Size(buttonWidth, 30);
            btnClearSearch.BackColor = Color.LightBlue;
            btnClearSearch.Font = new Font("Arial", 9F, FontStyle.Bold);
            btnClearSearch.ForeColor = Color.Black;
            btnClearSearch.Click += BtnClearSearch_Click;

            // Ajouter tous les contrôles au groupe
            gbSearch.Controls.AddRange(new Control[] {
        lblSearch, txtSearch,
        lblSearchFilter, cmbSearchFilter,
        lblSifaFilter, cmbSifaFilter,
        btnClearSearch
    });
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }
        private void CmbSearchFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void CmbSifaFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }
        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbSearchFilter.SelectedIndex = 0;
            cmbSifaFilter.SelectedIndex = 0;
            ApplyFilters();
        }
        private void ApplyFilters()
        {
            try
            {
                if (allLicensesCache == null)
                {
                    allLicensesCache = dbManager.GetAllLicenses();
                }

                var filteredLicenses = allLicensesCache.AsEnumerable();

                // Filtre par texte de recherche
                string searchText = txtSearch.Text.Trim();
                string searchFilter = cmbSearchFilter.SelectedItem?.ToString() ?? "الكل";

                if (!string.IsNullOrEmpty(searchText))
                {
                    switch (searchFilter)
                    {
                        case "المرجع":
                            filteredLicenses = filteredLicenses.Where(l =>
                                l.Reference != null && l.Reference.Contains(searchText));
                            break;
                        case "الاسم":
                            filteredLicenses = filteredLicenses.Where(l =>
                                l.Nom != null && l.Nom.Contains(searchText));
                            break;
                        case "النشاط":
                            filteredLicenses = filteredLicenses.Where(l =>
                                l.Activite != null && l.Activite.Contains(searchText));
                            break;
                        case "التاريخ":
                            filteredLicenses = filteredLicenses.Where(l =>
                                l.Date.ToString("yyyy/MM/dd").Contains(searchText));
                            break;
                        case "النوع":
                            filteredLicenses = filteredLicenses.Where(l =>
                                l.Type != null && l.Type.Contains(searchText));
                            break;
                        case "الكل":
                        default:
                            filteredLicenses = filteredLicenses.Where(l =>
                                (l.Reference != null && l.Reference.Contains(searchText)) ||
                                (l.Nom != null && l.Nom.Contains(searchText)) ||
                                (l.Activite != null && l.Activite.Contains(searchText)) ||
                                (l.Type != null && l.Type.Contains(searchText)) ||
                                l.Date.ToString("yyyy/MM/dd").Contains(searchText));
                            break;
                    }
                }

                // Filtre par الصفة
                string sifaFilter = cmbSifaFilter.SelectedItem?.ToString() ?? "الكل";
                if (sifaFilter != "الكل")
                {
                    filteredLicenses = filteredLicenses.Where(l => l.Sifa == sifaFilter);
                }

                // Appliquer les résultats filtrés au DataGridView
                var filteredList = filteredLicenses.ToList();

                dgvLicenses.SuspendLayout();
                dgvLicenses.DataSource = null;
                dgvLicenses.DataSource = filteredList;

                if (dgvLicenses.Columns.Count > 0)
                {
                    ConfigureColumns();
                    ApplyRowColors();
                }

                dgvLicenses.ResumeLayout();
                dgvLicenses.Refresh();

                // Mettre à jour le titre du groupe avec le nombre de résultats
                gbList.Text = $"قائمة التراخيص ({filteredList.Count} من {allLicensesCache.Count})";
            }
            catch (Exception ex)
            {
                dgvLicenses.ResumeLayout();
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateTopMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.RightToLeft = RightToLeft.Yes;
            menuStrip.BackColor = Color.FromArgb(240, 240, 240);
            menuStrip.Font = new Font("Arial", 10F, FontStyle.Bold);

            // Bouton avec 3 points (⋯)
            menuButton = new ToolStripDropDownButton();
            menuButton.Text = "⋯"; // Caractère 3 points
            menuButton.Font = new Font("Arial", 14F, FontStyle.Bold);
            menuButton.AutoSize = false;
            menuButton.Size = new Size(50, 30);

            // Options du menu
            ToolStripMenuItem licensesOption = new ToolStripMenuItem();
            licensesOption.Text = "عرض التراخيص";
            licensesOption.Font = new Font("Arial", 10F);
            licensesOption.Click += (s, e) => ShowLicensesView();

            ToolStripMenuItem activitiesOption = new ToolStripMenuItem();
            activitiesOption.Text = "إحصائيات الأنشطة";
            activitiesOption.Font = new Font("Arial", 10F);
            activitiesOption.Click += (s, e) => ShowActivitiesView();

            menuButton.DropDownItems.AddRange(new ToolStripItem[] { licensesOption, activitiesOption });
            menuStrip.Items.Add(menuButton);

            this.MainMenuStrip = menuStrip;
        }

        // ✅ NOUVEAU: Configurer les contrôles du groupe d'ajout avec positions adaptatives
        private void SetupAddGroupControls()
        {
            // Nettoyer les contrôles existants
            gbAdd.Controls.Clear();

            int formWidth = gbAdd.Width;
            int margin = 20;
            int controlHeight = 25;
            int labelWidth = 80;
            int comboWidth = 200;
            int textBoxWidth = 180;

            // ✅ LIGNE 1: Type, Référence, Nom
            // Type de licence
            Label lblType = new Label();
            lblType.Text = "النوع:";
            lblType.Location = new Point(formWidth - margin - labelWidth, 30);
            lblType.Size = new Size(labelWidth, controlHeight);
            lblType.TextAlign = ContentAlignment.MiddleRight;
            lblType.Font = new Font("Arial", 10F);

            cmbType = new ComboBox();
            cmbType.Items.Clear();
            cmbType.Items.AddRange(new string[] {
        "تصريح",
        "رخصة",
        "قرار تحويل",
        "طلب الإلغاء"
    });
            cmbType.Location = new Point(formWidth - margin - labelWidth - comboWidth - 10, 30);
            cmbType.Size = new Size(comboWidth, controlHeight);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.RightToLeft = RightToLeft.Yes;
            cmbType.Font = new Font("Arial", 10F);
            cmbType.SelectedIndexChanged += CmbType_SelectedIndexChanged;

            // Référence
            Label lblReference = new Label();
            lblReference.Text = "المرجع:";
            lblReference.Location = new Point(formWidth - margin - labelWidth - comboWidth - 20 - labelWidth, 30);
            lblReference.Size = new Size(labelWidth, controlHeight);
            lblReference.TextAlign = ContentAlignment.MiddleRight;
            lblReference.Font = new Font("Arial", 10F);

            txtReference = new TextBox();
            txtReference.Location = new Point(formWidth - margin - labelWidth - comboWidth - 20 - labelWidth - textBoxWidth - 10, 30);
            txtReference.Size = new Size(textBoxWidth, controlHeight);
            txtReference.RightToLeft = RightToLeft.Yes;
            txtReference.Font = new Font("Arial", 10F);

            // Nom
            Label lblNom = new Label();
            lblNom.Text = "الاسم:";
            lblNom.Location = new Point(formWidth - margin - labelWidth - comboWidth - 20 - labelWidth - textBoxWidth - 20 - labelWidth, 30);
            lblNom.Size = new Size(labelWidth, controlHeight);
            lblNom.TextAlign = ContentAlignment.MiddleRight;
            lblNom.Font = new Font("Arial", 10F);

            TextBox txtNom = new TextBox();
            txtNom.Name = "txtNom";
            txtNom.Location = new Point(370, 30); // ✅ Position fixe à gauche
            txtNom.Size = new Size(textBoxWidth, controlHeight);
            txtNom.RightToLeft = RightToLeft.Yes;
            txtNom.Font = new Font("Arial", 10F);

            // ✅ LIGNE 2: Activités (étendue sur toute la largeur)
            Label lblActivites = new Label();
            lblActivites.Text = "الأنشطة:";
            lblActivites.Location = new Point(formWidth - margin - labelWidth, 65);
            lblActivites.Size = new Size(labelWidth, controlHeight);
            lblActivites.TextAlign = ContentAlignment.MiddleRight;
            lblActivites.Font = new Font("Arial", 10F);

            cmbType_Activite = new ComboBox();
            cmbType_Activite.Name = "cmbType_Activite";
            cmbType_Activite.Location = new Point(40, 65);
            cmbType_Activite.Size = new Size(formWidth - margin - labelWidth - 60, controlHeight); // ✅ Largeur étendue
            cmbType_Activite.DropDownStyle = ComboBoxStyle.DropDown;
            cmbType_Activite.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbType_Activite.AutoCompleteSource = AutoCompleteSource.ListItems;
            cmbType_Activite.RightToLeft = RightToLeft.Yes;
            cmbType_Activite.Font = new Font("Arial", 10F);

            // ✅ LIGNE 3: Sifa et Date
            Label lblSifa = new Label();
            lblSifa.Text = "الصفة:";
            lblSifa.Location = new Point(formWidth - margin - labelWidth, 100);
            lblSifa.Size = new Size(labelWidth, controlHeight);
            lblSifa.TextAlign = ContentAlignment.MiddleRight;
            lblSifa.Font = new Font("Arial", 10F);

            cmb_personne = new ComboBox();
            cmb_personne.Name = "cmb_personne";
            cmb_personne.Location = new Point(formWidth - margin - labelWidth - comboWidth - 10, 100);
            cmb_personne.Size = new Size(comboWidth, controlHeight);
            cmb_personne.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb_personne.Items.AddRange(new string[] { "شخص ذاتي", "شخص معنوي" });
            cmb_personne.RightToLeft = RightToLeft.Yes;
            cmb_personne.Font = new Font("Arial", 10F);

            // Date actuelle
            lblCurrentDate = new Label();
            lblCurrentDate.Location = new Point(40, 100);
            lblCurrentDate.Size = new Size(300, controlHeight);
            lblCurrentDate.Font = new Font("Arial", 11F, FontStyle.Bold);
            lblCurrentDate.ForeColor = Color.Blue;
            lblCurrentDate.TextAlign = ContentAlignment.MiddleLeft; // ✅ Alignement à gauche

            // ✅ LIGNE 4: Boutons (centrés et bien espacés)
            int buttonWidth = 90;
            int buttonHeight = 35;
            int totalButtonsWidth = buttonWidth * 4 + 30; // 4 boutons + 3 espaces
            int startX = (formWidth - totalButtonsWidth) / 2; // ✅ Centrer les boutons

            btnAdd = new Button();
            btnAdd.Text = "إضافة";
            btnAdd.Location = new Point(startX, 135);
            btnAdd.Size = new Size(buttonWidth, buttonHeight);
            btnAdd.BackColor = Color.LightGreen;
            btnAdd.Font = new Font("Arial", 10F, FontStyle.Bold);
            btnAdd.ForeColor = Color.Black;
            btnAdd.Click += BtnAdd_Click;

            btnmodify = new Button();
            btnmodify.Text = "تعديل";
            btnmodify.Location = new Point(startX + buttonWidth + 10, 135);
            btnmodify.Size = new Size(buttonWidth, buttonHeight);
            btnmodify.BackColor = Color.BlueViolet;
            btnmodify.Font = new Font("Arial", 10F, FontStyle.Bold);
            btnmodify.ForeColor = Color.White;
            btnmodify.Click += btnModifier_Click;

            btnCancel = new Button();
            btnCancel.Text = "مسح";
            btnCancel.Location = new Point(startX + (buttonWidth + 10) * 2, 135);
            btnCancel.Size = new Size(buttonWidth, buttonHeight);
            btnCancel.BackColor = Color.LightCoral;
            btnCancel.Font = new Font("Arial", 10F, FontStyle.Bold);
            btnCancel.ForeColor = Color.Black;
            btnCancel.Click += BtnCancel_Click;

            btnDelete = new Button();
            btnDelete.Text = "حذف";
            btnDelete.Location = new Point(startX + (buttonWidth + 10) * 3, 135);
            btnDelete.Size = new Size(buttonWidth, buttonHeight);
            btnDelete.BackColor = Color.Red;
            btnDelete.Font = new Font("Arial", 10F, FontStyle.Bold);
            btnDelete.ForeColor = Color.White;
            btnDelete.Click += BtnDelete_Click;

            // Ajouter tous les contrôles
            gbAdd.Controls.AddRange(new Control[] {
        lblType, cmbType,
        lblReference, txtReference,
        lblNom, txtNom,
        lblActivites, cmbType_Activite,
        lblSifa, cmb_personne,
        lblCurrentDate,
        btnAdd, btnmodify, btnCancel, btnDelete
    });
        }

        // ✅ NOUVEAU: Configurer les contrôles des statistiques
        private void SetupStatsControls()
        {
            gbStats.Controls.Clear();

            lblQuarterlyStats = new Label();
            lblQuarterlyStats.Location = new Point(20, 25);
            lblQuarterlyStats.Size = new Size(gbStats.Width - 40, 25);
            lblQuarterlyStats.Font = new Font("Arial", 10F, FontStyle.Bold);
            lblQuarterlyStats.ForeColor = Color.DarkBlue;
            lblQuarterlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ Alignement à droite
            lblQuarterlyStats.RightToLeft = RightToLeft.Yes;

            lblYearlyStats = new Label();
            lblYearlyStats.Location = new Point(20, 55);
            lblYearlyStats.Size = new Size(gbStats.Width - 40, 25);
            lblYearlyStats.Font = new Font("Arial", 10F, FontStyle.Bold);
            lblYearlyStats.ForeColor = Color.DarkGreen;
            lblYearlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ Alignement à droite
            lblYearlyStats.RightToLeft = RightToLeft.Yes;

            gbStats.Controls.AddRange(new Control[] { lblQuarterlyStats, lblYearlyStats });
        }

        // ✅ AMÉLIORATION: Méthode pour ajuster automatiquement lors du redimensionnement
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (gbAdd != null && gbSearch != null && gbStats != null && gbList != null)
            {
                // Réajuster les tailles des groupes
                gbAdd.Size = new Size(this.ClientSize.Width - 40, 180);
                gbSearch.Size = new Size(this.ClientSize.Width - 40, 60);
                gbStats.Size = new Size(this.ClientSize.Width - 40, 100);
                gbList.Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 450);

                // ✅ AJOUT : Redimensionner les labels des statistiques ET maintenir l'alignement à droite
                if (lblQuarterlyStats != null)
                {
                    lblQuarterlyStats.Size = new Size(gbStats.Width - 40, 25);
                    lblQuarterlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ MAINTENIR L'ALIGNEMENT
                }

                if (lblYearlyStats != null)
                {
                    lblYearlyStats.Size = new Size(gbStats.Width - 40, 25);
                    lblYearlyStats.TextAlign = ContentAlignment.MiddleRight; // ✅ MAINTENIR L'ALIGNEMENT
                }
            }
        }
        private void ShowLicensesView()
        {
            isShowingLicenses = true;
            gbList.Text = "قائمة التراخيص";
            dgvLicenses.Visible = true;
            dgvActivities.Visible = false;
            LoadData(); // Charger les données des licences
        }
        private void LoadActivitiesStatistics()
        {
            try
            {
                var activitiesStats = dbManager.GetActivitiesStatistics();
                dgvActivities.DataSource = null;
                dgvActivities.DataSource = activitiesStats;

                if (dgvActivities.Columns.Count > 0)
                {
                    // Configuration des colonnes pour les statistiques d'activités
                    if (dgvActivities.Columns.Contains("النشاط"))
                    {
                        dgvActivities.Columns["النشاط"].HeaderText = "النشاط";
                        dgvActivities.Columns["النشاط"].FillWeight = 60;
                        dgvActivities.Columns["النشاط"].DefaultCellStyle.Font = new Font("Arial", 11F);
                        dgvActivities.Columns["النشاط"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }

                    if (dgvActivities.Columns.Contains("العدد"))
                    {
                        dgvActivities.Columns["العدد"].HeaderText = "العدد";
                        dgvActivities.Columns["العدد"].FillWeight = 20;
                        dgvActivities.Columns["العدد"].DefaultCellStyle.Font = new Font("Arial", 12F, FontStyle.Bold);
                        dgvActivities.Columns["العدد"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        dgvActivities.Columns["العدد"].DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                    }

                    if (dgvActivities.Columns.Contains("السنة"))
                    {
                        dgvActivities.Columns["السنة"].HeaderText = "السنة";
                        dgvActivities.Columns["السنة"].FillWeight = 20;
                        dgvActivities.Columns["السنة"].DefaultCellStyle.Font = new Font("Arial", 11F, FontStyle.Bold);
                        dgvActivities.Columns["السنة"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        dgvActivities.Columns["السنة"].DefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
                    }

                    dgvActivities.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }

                dgvActivities.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل إحصائيات الأنشطة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ✅ NOUVEAU: Afficher la vue des statistiques d'activités
        private void ShowActivitiesView()
        {
            isShowingLicenses = false;
            gbList.Text = "إحصائيات الأنشطة حسب السنة";
            dgvLicenses.Visible = false;
            dgvActivities.Visible = true;
            LoadActivitiesStatistics(); // ✅ NOUVEAU: Charger les statistiques d'activités
        }
        private void SetupDataGridViews()
        {
            // DataGridView principal (Licences)
            dgvLicenses = new DataGridView();
            ConfigureDataGridView(dgvLicenses);
            dgvLicenses.CellDoubleClick += DgvLicenses_CellDoubleClick;
            dgvLicenses.SelectionChanged += DgvLicenses_SelectionChanged;
            dgvLicenses.CellFormatting += DgvLicenses_CellFormatting;

            // ✅ NOUVEAU: DataGridView pour les statistiques d'activités
            dgvActivities = new DataGridView();
            ConfigureDataGridView(dgvActivities);
            dgvActivities.Visible = false; // Masqué par défaut

            gbList.Controls.AddRange(new Control[] { dgvLicenses, dgvActivities });
        }
        private void ConfigureDataGridView(DataGridView dgv)
        {
            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.RightToLeft = RightToLeft.Yes;

            // Configuration visuelle améliorée
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.FromArgb(200, 200, 200);
            dgv.BorderStyle = BorderStyle.Fixed3D;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = true;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 40; // ✅ AGRANDI
            dgv.RowTemplate.Height = 32; // ✅ AGRANDI

            // Style des en-têtes - PLUS GROS
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(70, 130, 180);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 12F, FontStyle.Bold); // ✅ PLUS GROS
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Style par défaut des cellules - PLUS GROS
            dgv.DefaultCellStyle.Font = new Font("Arial", 11F); // ✅ PLUS GROS
            dgv.DefaultCellStyle.Padding = new Padding(5); // ✅ PLUS D'ESPACE
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        }

        private void DateTimer_Tick(object sender, EventArgs e)
        {
            lblCurrentDate.Text = $"التاريخ الحالي: {DateTime.Now:yyyy/MM/dd HH:mm:ss}";
        }
        private void DgvLicenses_SelectionChanged(object sender, EventArgs e)
        {
            // Les boutons restent toujours activés
            // Seule exception : si on est en mode édition, garder les restrictions actuelles

            if (btnmodify.Text == "حفظ التعديل")
            {
                // En mode édition, garder les boutons Add et Delete désactivés
                btnAdd.Enabled = false;
                btnDelete.Enabled = false;
                btnmodify.Enabled = true; // Le bouton modify reste activé pour sauvegarder
            }
            else
            {
                // Mode normal : tous les boutons sont activés
                btnAdd.Enabled = true;
                btnmodify.Enabled = true;
                btnDelete.Enabled = true;
                btnCancel.Enabled = true;
            }
        }
        private void DgvLicenses_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvLicenses.Columns[e.ColumnIndex].Name == "Date" && e.Value != null)
            {
                if (e.Value is DateTime dateValue)
                {
                    e.Value = dateValue.ToString("yyyy/MM/dd HH:mm");
                    e.FormattingApplied = true;
                }
            }

            // Formatter le statut avec des icônes textuelles
            if (dgvLicenses.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status == "نشط")
                {
                    e.Value = "● نشط";
                }
                else if (status == "ملغي")
                {
                    e.Value = "✕ ملغي";
                }
                e.FormattingApplied = true;
            }
        }
        // Modifier la méthode BtnAdd_Click dans la section "طلب الإلغاء"
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string selectedType = cmbType.SelectedItem?.ToString();
            Control txtNom = gbAdd.Controls["txtNom"];

            // Validation adaptée selon le type sélectionné
            if (selectedType == "طلب الإلغاء")
            {
                // Pour طلب الإلغاء, au minimum la référence est obligatoire
                if (string.IsNullOrWhiteSpace(txtReference.Text))
                {
                    MessageBox.Show("يرجى إدخال المرجع", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Vérifier si la référence existe ET est active
                if (dbManager.ReferenceExistsAndActive(txtReference.Text.Trim()))
                {
                    var result = MessageBox.Show(
                        "هذا المرجع موجود بالفعل في النظام.\nهل تريد إلغاء هذا الترخيص؟",
                        "ترخيص موجود",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Annuler la licence existante
                        if (dbManager.CancelExistingLicense(txtReference.Text.Trim()))
                        {
                            MessageBox.Show("تم إلغاء الترخيص بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearForm();
                            LoadData();
                            UpdateStatistics();
                            return;
                        }
                        else
                        {
                            MessageBox.Show("فشل في إلغاء الترخيص", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // La référence n'existe pas ou est déjà annulée
                    string nomSaisi = string.IsNullOrWhiteSpace(txtNom.Text) ?
                        PromptForName("هذا المرجع غير موجود في النظام أو مُلغى بالفعل.\nيرجى إدخال اسم صاحب الترخيص:") :
                        txtNom.Text.Trim();

                    if (string.IsNullOrEmpty(nomSaisi))
                    {
                        return;
                    }

                    // Ajouter un nouveau طلب الإلغاء
                    var cancelLicense = new License
                    {
                        Type = "طلب الإلغاء",
                        Reference = txtReference.Text.Trim(),
                        Date = DateTime.Now,
                        Status = "ملغي",
                        SubType = "-",
                        Sifa = cmb_personne.SelectedItem?.ToString() ?? "شخص ذاتي",
                        Nom = nomSaisi,
                        Activite = cmbType_Activite.Text?.Trim() ?? "",
                        DateAnnulation = DateTime.Now
                    };

                    if (dbManager.AddLicense(cancelLicense))
                    {
                        MessageBox.Show("تم إضافة طلب الإلغاء بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearForm();
                        LoadData();
                        UpdateStatistics();
                        return;
                    }
                    else
                    {
                        MessageBox.Show("فشل في إضافة طلب الإلغاء", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            else if (selectedType == "قرار تحويل")
            {
                // Pour قرار تحويل, référence et nom sont requis au minimum
                if (string.IsNullOrWhiteSpace(txtReference.Text) || string.IsNullOrWhiteSpace(txtNom.Text))
                {
                    MessageBox.Show("يرجى إدخال المرجع والاسم على الأقل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Vérifier si la référence existe déjà
                if (dbManager.ReferenceExists(txtReference.Text.Trim()))
                {
                    // La référence existe → mettre à jour seulement le nom ET l'activité
                    if (dbManager.UpdateNameAndActivityByReference(txtReference.Text.Trim(), txtNom.Text.Trim(), cmbType_Activite.Text?.Trim() ?? ""))
                    {
                        MessageBox.Show("تم تحديث البيانات بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearForm();
                        LoadData();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show("فشل في تحديث البيانات", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // La référence n'existe pas → créer une nouvelle ligne
                    var transferLicense = new License
                    {
                        Type = "قرار تحويل",
                        Reference = txtReference.Text.Trim(),
                        Date = DateTime.Now,
                        Status = "نشط",
                        SubType = "-",
                        Sifa = cmb_personne.SelectedItem?.ToString() ?? "شخص ذاتي",
                        Nom = txtNom.Text.Trim(),
                        Activite = cmbType_Activite.Text?.Trim() ?? ""
                    };

                    if (dbManager.AddLicense(transferLicense))
                    {
                        MessageBox.Show("تم إضافة قرار التحويل بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearForm();
                        LoadData();
                        UpdateStatistics();
                    }
                    else
                    {
                        MessageBox.Show("فشل في إضافة قرار التحويل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else // Pour تصريح et رخصة
            {
                // Validation de base - référence obligatoire, nom recommandé
                if (cmbType.SelectedItem == null || string.IsNullOrWhiteSpace(txtReference.Text))
                {
                    MessageBox.Show("يرجى اختيار النوع وإدخال المرجع على الأقل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check for duplicate reference
                if (dbManager.ReferenceExists(txtReference.Text.Trim()))
                {
                    MessageBox.Show("هذا المرجع موجود بالفعل، يرجى استخدام مرجع آخر", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string status = "نشط";
                string subType = "-";
                string sifa = cmb_personne.SelectedItem?.ToString() ?? "شخص ذاتي";
                string nom = txtNom.Text?.Trim() ?? "";
                string reference = txtReference.Text?.Trim() ?? "";
                string activite = cmbType_Activite.Text?.Trim() ?? "";

                // Si le type est "رخصة", afficher le message de choix
                if (selectedType == "رخصة")
                {
                    subType = CustomMessageBox.Show(
                        "اختر نوع الرخصة",
                        "يرجى اختيار نوع الرخصة:",
                        "دفتر التحملات",
                        "بحت المنافع و الاضرار"
                    );
                }

                var license = new License
                {
                    Type = selectedType,
                    Reference = reference,
                    Date = DateTime.Now,
                    Status = status,
                    SubType = subType,
                    Sifa = sifa,
                    Nom = nom,
                    Activite = activite
                };

                if (dbManager.AddLicense(license))
                {
                    MessageBox.Show("تم إضافة السجل بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                    LoadData();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("فشل في إضافة السجل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // NOUVELLE MÉTHODE: Créer une méthode pour demander le nom à l'utilisateur
        private string PromptForName(string message)
        {
            Form promptForm = new Form();
            promptForm.Text = "إدخال الاسم";
            promptForm.Size = new Size(400, 180);
            promptForm.StartPosition = FormStartPosition.CenterParent;
            promptForm.RightToLeft = RightToLeft.Yes;
            promptForm.RightToLeftLayout = true;
            promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            promptForm.MaximizeBox = false;
            promptForm.MinimizeBox = false;

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.Location = new Point(20, 20);
            messageLabel.Size = new Size(350, 40);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;

            Label nameLabel = new Label();
            nameLabel.Text = "الاسم:";
            nameLabel.Location = new Point(320, 70);
            nameLabel.Size = new Size(50, 23);
            nameLabel.TextAlign = ContentAlignment.MiddleRight;

            TextBox nameTextBox = new TextBox();
            nameTextBox.Location = new Point(100, 70);
            nameTextBox.Size = new Size(200, 23);
            nameTextBox.RightToLeft = RightToLeft.Yes;

            Button okButton = new Button();
            okButton.Text = "موافق";
            okButton.Location = new Point(200, 110);
            okButton.Size = new Size(80, 30);
            okButton.DialogResult = DialogResult.OK;

            Button cancelButton = new Button();
            cancelButton.Text = "إلغاء";
            cancelButton.Location = new Point(110, 110);
            cancelButton.Size = new Size(80, 30);
            cancelButton.DialogResult = DialogResult.Cancel;

            promptForm.Controls.AddRange(new Control[] { messageLabel, nameLabel, nameTextBox, okButton, cancelButton });

            // Définir le bouton par défaut et permettre Enter/Escape
            promptForm.AcceptButton = okButton;
            promptForm.CancelButton = cancelButton;

            string result = "";
            okButton.Click += (sender, e) => {
                if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    result = nameTextBox.Text.Trim();
                    promptForm.Close();
                }
                else
                {
                    MessageBox.Show("يرجى إدخال الاسم", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            cancelButton.Click += (sender, e) => {
                result = "";
                promptForm.Close();
            };

            // Permettre à l'utilisateur de valider avec Enter
            nameTextBox.KeyDown += (sender, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    okButton.PerformClick();
                }
            };

            if (promptForm.ShowDialog() == DialogResult.OK)
            {
                return result;
            }

            return "";
        }
        private void CmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedType = cmbType.SelectedItem?.ToString();

            // ✅ SOLUTION FINALE: TOUS LES CHAMPS RESTENT TOUJOURS ACTIVÉS
            txtReference.Enabled = true;
            Control txtNom = gbAdd.Controls["txtNom"];
            if (txtNom != null)
            {
                txtNom.Enabled = true;
            }
            cmbType_Activite.Enabled = true;  // ✅ TOUJOURS ACTIVÉ
            cmb_personne.Enabled = true;      // ✅ TOUJOURS ACTIVÉ

            // Aucune désactivation de champs - tous restent accessibles
            // L'utilisateur peut remplir tous les champs selon ses besoins
            // La validation dans BtnAdd_Click déterminera quels champs sont 
            // réellement requis selon le type sélectionné

            // Optionnel : Vous pouvez ajouter des valeurs par défaut selon le type
            switch (selectedType)
            {
                case "طلب الإلغاء":
                    // Pas de valeurs par défaut spéciales
                    break;
                case "قرار تحويل":
                    // Pas de valeurs par défaut spéciales
                    break;
                case "تصريح":
                    // Vous pouvez définir des valeurs par défaut si nécessaire
                    break;
                case "رخصة":
                    // Vous pouvez définir des valeurs par défaut si nécessaire
                    break;
                default:
                    break;
            }
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvLicenses.SelectedRows.Count == 0)
            {
                MessageBox.Show("يرجى اختيار ترخيص للحذف", "تحذير", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("هل أنت متأكد من حذف هذا الترخيص نهائياً؟\nهذا الإجراء لا يمكن التراجع عنه!",
                "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var reference = dgvLicenses.SelectedRows[0].Cells["Reference"].Value.ToString();

                if (dbManager.DeleteLicense(reference))
                {
                    MessageBox.Show("تم حذف الترخيص بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("فشل في حذف الترخيص", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearForm()
        {
            cmbType.SelectedIndex = -1;
            txtReference.Clear();
            txtReference.Tag = null;
            cmb_personne.SelectedIndex = -1;
            cmbType_Activite.SelectedIndex = -1;

            Control txtNom = gbAdd.Controls["txtNom"];
            if (txtNom != null)
            {
                txtNom.Text = "";
            }

            // Réactiver tous les champs
            txtReference.Enabled = true;
            txtNom.Enabled = true;
            cmbType_Activite.Enabled = true;
            cmb_personne.Enabled = true;

            // Reset modify button if in edit mode
            if (btnmodify.Text == "حفظ التعديل")
            {
                btnmodify.Text = "تعديل";
                btnmodify.BackColor = Color.BlueViolet;
            }

            // ✅ CORRECTION : Activer TOUS les boutons
            btnAdd.Enabled = true;
            btnDelete.Enabled = true;
            btnmodify.Enabled = true;
            btnCancel.Enabled = true;
        }


        private void DgvLicenses_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var reference = dgvLicenses.Rows[e.RowIndex].Cells["Reference"].Value.ToString();
                var currentStatus = dgvLicenses.Rows[e.RowIndex].Cells["Status"].Value.ToString();

                if (currentStatus == "نشط")
                {
                    var result = MessageBox.Show("هل تريد إلغاء هذا الترخيص؟", "تأكيد الإلغاء",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // ✅ CORRECTION : Actualiser le DataGridView après cessation
                        if (dbManager.CancelExistingLicense(reference))
                        {
                            MessageBox.Show("تم إلغاء الترخيص بنجاح", "نجح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Actualiser immédiatement les données
                            LoadData();
                            UpdateStatistics();

                            // ✅ AJOUT : Forcer le rafraîchissement du DataGridView
                            dgvLicenses.Refresh();
                            dgvLicenses.Invalidate();
                        }
                        else
                        {
                            MessageBox.Show("فشل في إلغاء الترخيص", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("هذا الترخيص ملغي بالفعل", "معلومة", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void RefreshDataGrid()
        {
            try
            {
                // Suspendre le layout pour éviter le scintillement
                dgvLicenses.SuspendLayout();

                // Sauvegarder l'index de défilement actuel
                int currentFirstDisplayedScrollingRowIndex = dgvLicenses.FirstDisplayedScrollingRowIndex;

                LoadData();
                UpdateStatistics();

                // Restaurer la position de défilement si possible
                if (currentFirstDisplayedScrollingRowIndex >= 0 &&
                    currentFirstDisplayedScrollingRowIndex < dgvLicenses.Rows.Count)
                {
                    dgvLicenses.FirstDisplayedScrollingRowIndex = currentFirstDisplayedScrollingRowIndex;
                }

                // Reprendre le layout
                dgvLicenses.ResumeLayout();
                dgvLicenses.Refresh();

                // Forcer la mise à jour de l'affichage parent
                this.Refresh();
            }
            catch (Exception ex)
            {
                dgvLicenses.ResumeLayout();
                MessageBox.Show($"خطأ في تحديث البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadData()
        {
            try
            {
                // Sauvegarder la sélection actuelle
                string selectedReference = null;
                if (dgvLicenses.SelectedRows.Count > 0)
                {
                    selectedReference = dgvLicenses.SelectedRows[0].Cells["Reference"].Value?.ToString();
                }

                // Suspendre le layout pour éviter le scintillement
                dgvLicenses.SuspendLayout();

                // Recharger le cache
                allLicensesCache = dbManager.GetAllLicenses();

                // Appliquer les filtres actuels au lieu de charger toutes les données
                ApplyFilters();

                // Restaurer la sélection si possible
                RestoreSelection(selectedReference);

                // Reprendre le layout
                dgvLicenses.ResumeLayout();
                dgvLicenses.Refresh();
            }
            catch (Exception ex)
            {
                dgvLicenses.ResumeLayout();
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void UpdateStatistics()
        {
            try
            {
                var quarterlyStats = dbManager.GetQuarterlyStatistics();
                var yearlyStats = dbManager.GetYearlyStatistics();

                // Déterminer le trimestre actuel
                string currentQuarter = GetCurrentQuarterName();

                // ✅ CORRECTION : Vérifier que les labels existent avant de les mettre à jour
                if (lblQuarterlyStats != null)
                {
                    lblQuarterlyStats.Text = $"إحصائيات {currentQuarter}: " +
                        $"تصريح: {quarterlyStats["تصريح"]}, رخصة: {quarterlyStats["رخصة"]}, " +
                        $"قرار التحويل: {quarterlyStats["قرار التحويل"]}, قرار الإلغاء: {quarterlyStats["قرار الإلغاء"]}";

                    // ✅ AJOUT : Forcer le rafraîchissement
                    lblQuarterlyStats.Invalidate();
                    lblQuarterlyStats.Update();
                }

                if (lblYearlyStats != null)
                {
                    lblYearlyStats.Text = $"إحصائيات السنة الحالية ({DateTime.Now.Year}): " +
                        $"تصريح: {yearlyStats["تصريح"]}, رخصة: {yearlyStats["رخصة"]}, " +
                        $"قرار التحويل: {yearlyStats["قرار التحويل"]}, قرار الإلغاء: {yearlyStats["قرار الإلغاء"]}";

                    // ✅ AJOUT : Forcer le rafraîchissement
                    lblYearlyStats.Invalidate();
                    lblYearlyStats.Update();
                }

                // ✅ AJOUT : Forcer le rafraîchissement du groupe des statistiques
                if (gbStats != null)
                {
                    gbStats.Invalidate();
                    gbStats.Update();
                }

                // ✅ AJOUT : Logs pour déboguer (optionnel)
                Console.WriteLine($"Statistiques trimestrielles mises à jour: {lblQuarterlyStats?.Text}");
                Console.WriteLine($"Statistiques annuelles mises à jour: {lblYearlyStats?.Text}");
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message d'erreur dans les labels
                if (lblQuarterlyStats != null)
                    lblQuarterlyStats.Text = "خطأ في تحميل الإحصائيات الفصلية";

                if (lblYearlyStats != null)
                    lblYearlyStats.Text = "خطأ في تحميل الإحصائيات السنوية";

                MessageBox.Show($"خطأ في تحديث الإحصائيات: {ex.Message}", "خطأ",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private string GetCurrentQuarterName()
        {
            int currentMonth = DateTime.Now.Month;

            if (currentMonth >= 1 && currentMonth <= 3)
                return "الربع الأول (يناير-فبراير-مارس)";
            else if (currentMonth >= 4 && currentMonth <= 6)
                return "الربع الثاني (أبريل-مايو-يونيو)";
            else if (currentMonth >= 7 && currentMonth <= 9)
                return "الربع الثالث (يوليو-أغسطس-سبتمبر)";
            else
                return "الربع الرابع (أكتوبر-نوفمبر-ديسمبر)";
        }
        private void ConfigureColumns()
        {
            // Configuration détaillée des colonnes
            var columnConfigs = new Dictionary<string, (string HeaderText, int FillWeight, int MinWidth, bool Visible)>
        {
        { "Reference", ("المرجع", 15, 100, true) },
        { "Type", ("النوع", 12, 90, true) },
        { "Nom", ("الاسم", 18, 120, true) },
        { "Sifa", ("الصفة", 20, 80, true) },
        { "Date", ("تاريخ الإنشاء", 15, 110, true) },
        { "Status", ("الحالة", 8, 70, true) },
        { "SubType", ("النوع الفرعي", 22, 180, true) },
        { "DateAnnulation", ("تاريخ الإلغاء", 0, 50, false) } // Masquée avec FillWeight 0
    };

            foreach (var config in columnConfigs)
            {
                if (dgvLicenses.Columns.Contains(config.Key))
                {
                    var column = dgvLicenses.Columns[config.Key];
                    column.HeaderText = config.Value.HeaderText;
                    column.Visible = config.Value.Visible;

                    // Configuration spéciale pour les colonnes masquées
                    if (config.Value.Visible)
                    {
                        column.FillWeight = config.Value.FillWeight;
                        column.MinimumWidth = config.Value.MinWidth;
                    }
                    else
                    {
                        // Pour les colonnes masquées, définir une largeur fixe minimale
                        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        column.Width = 50;
                        column.MinimumWidth = 50;
                    }

                    // Configuration spéciale pour la colonne Date
                    if (config.Key == "Date")
                    {
                        column.DefaultCellStyle.Format = "yyyy/MM/dd HH:mm";
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }

                    // Configuration pour la colonne Status
                    if (config.Key == "Status")
                    {
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        column.DefaultCellStyle.Font = new Font("Arial", 9F, FontStyle.Bold);
                    }

                    // Configuration pour la colonne Reference
                    if (config.Key == "Reference")
                    {
                        column.DefaultCellStyle.Font = new Font("Arial", 9F, FontStyle.Bold);
                        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }
            }

            // Définir l'ordre des colonnes (exclure les colonnes masquées)
            string[] columnOrder = { "Reference", "Type", "Nom", "Sifa", "Date", "Status", "SubType" };
            for (int i = 0; i < columnOrder.Length; i++)
            {
                if (dgvLicenses.Columns.Contains(columnOrder[i]) && dgvLicenses.Columns[columnOrder[i]].Visible)
                {
                    dgvLicenses.Columns[columnOrder[i]].DisplayIndex = i;
                }
            }

            // Configurer l'auto-sizing pour les colonnes visibles seulement
            dgvLicenses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void FilterData(string filterType = "الكل")
        {
            try
            {
                var allLicenses = dbManager.GetAllLicenses();
                List<License> filteredLicenses;

                switch (filterType)
                {
                    case "نشط فقط":
                        filteredLicenses = allLicenses.Where(l => l.Status == "نشط").ToList();
                        break;
                    case "ملغي فقط":
                        filteredLicenses = allLicenses.Where(l => l.Status == "ملغي").ToList();
                        break;
                    case "تصريح":
                        filteredLicenses = allLicenses.Where(l => l.Type == "تصريح").ToList();
                        break;
                    case "رخصة":
                        filteredLicenses = allLicenses.Where(l => l.Type == "رخصة").ToList();
                        break;
                    default:
                        filteredLicenses = allLicenses;
                        break;
                }

                dgvLicenses.DataSource = null;
                dgvLicenses.DataSource = filteredLicenses;

                if (dgvLicenses.Columns.Count > 0)
                {
                    ConfigureColumns();
                    ApplyRowColors();
                }

                dgvLicenses.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصفية البيانات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ApplyRowColors()
        {
            foreach (DataGridViewRow row in dgvLicenses.Rows)
            {
                if (row.Cells["Status"].Value != null)
                {
                    string status = row.Cells["Status"].Value.ToString();
                    string type = row.Cells["Type"].Value?.ToString() ?? "";

                    if (status == "ملغي")
                    {
                        // Couleurs pour les licences annulées
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230); // Rose très clair
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 0, 0);     // Rouge foncé
                        row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 200, 200);
                        row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 0, 0);
                    }
                    else
                    {
                        // Couleurs différentes selon le type pour les licences actives
                        switch (type)
                        {
                            case "تصريح":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(230, 255, 230); // Vert très clair
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 120, 0);     // Vert foncé
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 255, 200);
                                break;
                            case "رخصة":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(230, 230, 255); // Bleu très clair
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 0, 150);     // Bleu foncé
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 200, 255);
                                break;
                            case "طلب تحويل":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 230); // Jaune très clair
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 100, 0);   // Orange foncé
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 255, 200);
                                break;
                            case "طلب الإلغاء":
                                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 230); // Orange très clair
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 75, 0);    // Orange foncé
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 220, 200);
                                break;
                            default:
                                row.DefaultCellStyle.BackColor = Color.White;
                                row.DefaultCellStyle.ForeColor = Color.Black;
                                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 220, 255);
                                break;
                        }
                        row.DefaultCellStyle.SelectionForeColor = Color.Black;
                    }
                }
            }
        }
        private void RestoreSelection(string selectedReference)
        {
            if (!string.IsNullOrEmpty(selectedReference))
            {
                foreach (DataGridViewRow row in dgvLicenses.Rows)
                {
                    if (row.Cells["Reference"].Value?.ToString() == selectedReference)
                    {
                        row.Selected = true;
                        // Faire défiler vers la ligne sélectionnée si nécessaire
                        if (row.Index >= 0)
                        {
                            dgvLicenses.FirstDisplayedScrollingRowIndex = row.Index;
                        }
                        break;
                    }
                }
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Arrêter et disposer du timer
            dateTimer?.Stop();
            dateTimer?.Dispose();

            // Nettoyer les événements
            CleanupEventHandlers();

            // Fermer la connexion à la base de données si nécessaire
            dbManager = null;

            base.OnFormClosed(e);
        }
        private void Form1_Load(object sender, EventArgs e)
        {


            LoadActivitiesFromFile();


        }
        private void LoadActivitiesFromFile()
        {
            // Try multiple possible locations for the file
            string[] possiblePaths = {
        Path.Combine(Application.StartupPath, "Resources", "list activité.txt"),
        Path.Combine(Environment.CurrentDirectory, "Resources", "list activité.txt"),
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", "list activité.txt"),
        "Resources\\list activité.txt", // Relative path
        "list activité.txt" // Same directory as executable
    };

            string filePath = possiblePaths.FirstOrDefault(File.Exists);

            if (filePath != null)
            {
                try
                {
                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);

                    cmbType_Activite.Items.Clear(); // Clear existing items

                    foreach (var line in lines.Skip(1)) // Skip header
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(',');
                        if (parts.Length >= 1)
                        {
                            string title = parts[0].Trim().Trim('"');
                            if (!string.IsNullOrWhiteSpace(title) && !cmbType_Activite.Items.Contains(title))
                            {
                                cmbType_Activite.Items.Add(title);
                            }
                        }
                    }

                    Console.WriteLine($"تم تحميل {cmbType_Activite.Items.Count} نشاط من الملف: {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في قراءة ملف الأنشطة:\n{ex.Message}", "خطأ",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                string searchedPaths = string.Join("\n", possiblePaths);
                MessageBox.Show($"لم يتم العثور على ملف الأنشطة في المواقع التالية:\n{searchedPaths}",
                              "ملف الأنشطة غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
    public static class CustomMessageBox
    {
        public static string Show(string title, string message, string choice1, string choice2)
        {
            Form customForm = new Form();
            customForm.Text = title;
            customForm.Size = new Size(400, 200);
            customForm.StartPosition = FormStartPosition.CenterParent;
            customForm.RightToLeft = RightToLeft.Yes;
            customForm.RightToLeftLayout = true;
            customForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            customForm.MaximizeBox = false;
            customForm.MinimizeBox = false;

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.Location = new Point(20, 20);
            messageLabel.Size = new Size(350, 40);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;

            Button btn1 = new Button();
            btn1.Text = choice1;
            btn1.Location = new Point(80, 80);
            btn1.Size = new Size(100, 40);
            btn1.DialogResult = DialogResult.OK;
            btn1.Tag = choice1;

            Button btn2 = new Button();
            btn2.Text = choice2;
            btn2.Location = new Point(200, 80);
            btn2.Size = new Size(100, 40);
            btn2.DialogResult = DialogResult.Cancel;
            btn2.Tag = choice2;

            customForm.Controls.AddRange(new Control[] { messageLabel, btn1, btn2 });

            string result = "-";
            btn1.Click += (sender, e) => { result = choice1; customForm.Close(); };
            btn2.Click += (sender, e) => { result = choice2; customForm.Close(); };

            customForm.ShowDialog();
            return result;
        }
    }
    // Classe pour représenter une licence
    public class License
    {
        public string Reference { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string SubType { get; set; }
        public string Sifa { get; set; }
        public string Activite { get; set; }
        public string Nom { get; set; }
        public DateTime? DateAnnulation { get; set; } // NOUVEAU: Propriété pour la date d'annulation
    }

    // Gestionnaire de base de données
    public class DatabaseManager
    {
        private string connectionString;

        public DatabaseManager()
        {
            connectionString = "Data Source=licenses.db;Version=3;";
        }
        public List<dynamic> GetActivitiesStatistics()
        {
            var activitiesStats = new List<dynamic>();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Requête pour obtenir les statistiques d'activités par année
                    string query = @"
                SELECT 

                    CASE 
                        WHEN Activite IS NULL OR Activite = '' THEN 'غير محدد'
                        ELSE Activite 
                    END as النشاط,
                    COUNT(*) as العدد,
                    strftime('%Y', Date) as السنة
                FROM Licenses 
                WHERE Status = 'نشط'
                GROUP BY 
                    CASE 
                        WHEN Activite IS NULL OR Activite = '' THEN 'غير محدد'
                        ELSE Activite 
                    END,
                    strftime('%Y', Date)
                ORDER BY السنة DESC, العدد DESC";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activitiesStats.Add(new
                            {
                                النشاط = reader["النشاط"].ToString(),
                                العدد = Convert.ToInt32(reader["العدد"]),
                                السنة = reader["السنة"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner une liste vide avec un message d'erreur
                activitiesStats.Add(new
                {
                    النشاط = "خطأ في تحميل البيانات",
                    العدد = 0,
                    السنة = DateTime.Now.Year.ToString()
                });
            }

            return activitiesStats;
        }
        public bool UpdateNameAndActivityByReference(string reference, string newName, string newActivity)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Licenses SET Nom = @NewName, Activite = @NewActivity WHERE Reference = @Reference";

                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@NewName", newName);
                        command.Parameters.AddWithValue("@NewActivity", newActivity);
                        command.Parameters.AddWithValue("@Reference", reference);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool ReferenceExistsForType(string reference, string type)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Licenses WHERE Reference = @Reference AND Type = @Type";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Reference", reference);
                        command.Parameters.AddWithValue("@Type", type);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        public bool UpdateNameByReference(string reference, string newName)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Licenses SET Nom = @NewName WHERE Reference = @Reference";

                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@NewName", newName);
                        command.Parameters.AddWithValue("@Reference", reference);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                return false;
            }
        }
        public bool ReferenceExistsAndActive(string reference)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Licenses WHERE Reference = @Reference AND Status = 'نشط'";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Reference", reference);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        public bool CancelExistingLicense(string reference)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Vérifier si la licence existe et est active
                        string checkQuery = "SELECT COUNT(*) FROM Licenses WHERE Reference = @Reference AND Status = 'نشط'";
                        using (var checkCommand = new SQLiteCommand(checkQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@Reference", reference);
                            int activeCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                            if (activeCount == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // CORRECTION : Juste mettre à jour le statut et la date d'annulation
                        string updateQuery = "UPDATE Licenses SET Status = 'ملغي', DateAnnulation = @DateAnnulation WHERE Reference = @Reference AND Status = 'نشط'";
                        using (var command = new SQLiteCommand(updateQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Reference", reference);
                            command.Parameters.AddWithValue("@DateAnnulation", DateTime.Now);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                return true;
                            }
                            else
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // 1) Créer la table si elle n'existe pas
                const string createTableQuery = @"
        CREATE TABLE IF NOT EXISTS Licenses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Type TEXT NOT NULL,
            Reference TEXT NOT NULL,
            Date DATETIME NOT NULL,
            Status TEXT NOT NULL DEFAULT 'نشط',
            SubType TEXT DEFAULT '-',
            Sifa TEXT DEFAULT 'شخص ذاتي',
            Nom TEXT DEFAULT '',
            Activite TEXT DEFAULT '',
            DateAnnulation DATETIME NULL
        );";
                using (var cmd = new SQLiteCommand(createTableQuery, connection))
                    cmd.ExecuteNonQuery();

                // 2) Vérifier les colonnes existantes
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = new SQLiteCommand("PRAGMA table_info(Licenses);", connection))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                        existing.Add(rd["name"].ToString());
                }

                // 3) Ajouter les colonnes manquantes (si la table existait déjà avec un ancien schéma)
                var needed = new[]
                {
            "SubType TEXT DEFAULT '-'",
            "Sifa TEXT DEFAULT 'شخص ذاتي'",
            "Nom TEXT DEFAULT ''",
            "Activite TEXT DEFAULT ''",
            "DateAnnulation DATETIME NULL"
        };

                using (var tx = connection.BeginTransaction())
                {
                    foreach (var col in needed)
                    {
                        var name = col.Split(' ')[0]; // le nom de colonne
                        if (!existing.Contains(name))
                        {
                            using (var cmd = new SQLiteCommand($"ALTER TABLE Licenses ADD COLUMN {col};", connection, tx))
                                cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }

                // 4) (Optionnel) Index utile
                using (var cmd = new SQLiteCommand(
                    "CREATE INDEX IF NOT EXISTS IX_Licenses_Reference ON Licenses(Reference);",
                    connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool UpdateLicense(License license, string originalReference)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = @"UPDATE Licenses 
                                 SET Type = @Type, Reference = @Reference, Date = @Date, Status = @Status, 
                                     SubType = @SubType, Sifa = @Sifa, Nom = @Nom, DateAnnulation = @DateAnnulation
                                 WHERE Reference = @OriginalReference";

                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Type", license.Type);
                        command.Parameters.AddWithValue("@Reference", license.Reference);
                        command.Parameters.AddWithValue("@Date", license.Date);
                        command.Parameters.AddWithValue("@Status", license.Status);
                        command.Parameters.AddWithValue("@SubType", license.SubType ?? "-");
                        command.Parameters.AddWithValue("@OriginalReference", originalReference);
                        command.Parameters.AddWithValue("@Sifa", license.Sifa ?? "شخص ذاتي");
                        command.Parameters.AddWithValue("@Nom", license.Nom ?? "");
                        // NOUVEAU: Mettre à jour DateAnnulation
                        command.Parameters.AddWithValue("@DateAnnulation",
                            license.DateAnnulation.HasValue ? (object)license.DateAnnulation.Value : DBNull.Value);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ReferenceExists(string reference, string excludeReference = null)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Licenses WHERE Reference = @Reference";

                    if (!string.IsNullOrEmpty(excludeReference))
                    {
                        query += " AND Reference != @ExcludeReference";
                    }

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Reference", reference);
                        if (!string.IsNullOrEmpty(excludeReference))
                        {
                            command.Parameters.AddWithValue("@ExcludeReference", excludeReference);
                        }

                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public bool AddLicense(License license)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string insertQuery = @"
                INSERT INTO Licenses (Type, Reference, Date, Status, SubType, Sifa, Nom, Activite, DateAnnulation)
                VALUES (@Type, @Reference, @Date, @Status, @SubType, @Sifa, @Nom, @Activite, @DateAnnulation)";

                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Type", license.Type);
                        command.Parameters.AddWithValue("@Reference", license.Reference);
                        command.Parameters.AddWithValue("@Date", license.Date);
                        command.Parameters.AddWithValue("@Status", license.Status);
                        command.Parameters.AddWithValue("@SubType", license.SubType ?? "-");
                        command.Parameters.AddWithValue("@Sifa", license.Sifa ?? "شخص ذاتي");
                        command.Parameters.AddWithValue("@Nom", license.Nom ?? "");
                        command.Parameters.AddWithValue("@Activite", license.Activite ?? "");  // ✅ NOUVEAU
                        command.Parameters.AddWithValue("@DateAnnulation",
                            license.DateAnnulation.HasValue ? (object)license.DateAnnulation.Value : DBNull.Value);

                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        public bool DeleteLicense(string reference)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM Licenses WHERE Reference = @Reference";

                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Reference", reference);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public List<License> GetAllLicenses()
        {
            var licenses = new List<License>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT * FROM Licenses ORDER BY Date DESC";

                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        licenses.Add(new License
                        {
                            Type = reader["Type"].ToString(),
                            Reference = reader["Reference"].ToString(),
                            Date = Convert.ToDateTime(reader["Date"]),
                            Status = reader["Status"].ToString(),
                            SubType = reader["SubType"]?.ToString() ?? "-",
                            Sifa = reader["Sifa"]?.ToString() ?? "شخص ذاتي",
                            Nom = reader["Nom"]?.ToString() ?? "",
                            Activite = reader["Activite"]?.ToString() ?? "",  // ✅ NOUVEAU
                            DateAnnulation = reader["DateAnnulation"] != DBNull.Value ?
                                (DateTime?)Convert.ToDateTime(reader["DateAnnulation"]) : null
                        });
                    }
                }
            }

            return licenses;
        }
        public Dictionary<string, int> GetQuarterlyStatistics()
        {
            var stats = new Dictionary<string, int>
    {
        { "تصريح", 0 },
        { "رخصة", 0 },
        { "قرار التحويل", 0 }, // ✅ CORRECTION: Changé de "قرار التحويل" vers "طلب تحويل"
        { "قرار الإلغاء", 0 }
    };

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var currentQuarter = GetCurrentQuarterMonths();

                // ✅ CORRECTION: Ajouter "طلب تحويل" dans la requête
                string selectActiveQuery = @"
            SELECT Type, COUNT(*) as Count
            FROM Licenses
            WHERE Type IN ('تصريح', 'رخصة', 'طلب تحويل')
            AND Status = 'نشط'
            AND CAST(strftime('%m', Date) AS INTEGER) >= @StartMonth
            AND CAST(strftime('%m', Date) AS INTEGER) <= @EndMonth
            AND strftime('%Y', Date) = @Year
            GROUP BY Type";

                using (var command = new SQLiteCommand(selectActiveQuery, connection))
                {
                    command.Parameters.AddWithValue("@StartMonth", currentQuarter.Item1);
                    command.Parameters.AddWithValue("@EndMonth", currentQuarter.Item2);
                    command.Parameters.AddWithValue("@Year", DateTime.Now.Year.ToString());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["Type"].ToString();
                            int count = Convert.ToInt32(reader["Count"]);

                            // ✅ CORRECTION: Mapper "طلب تحويل" vers "قرار التحويل" pour l'affichage
                            if (type == "طلب تحويل")
                            {
                                stats["قرار التحويل"] = count;
                            }
                            else if (stats.ContainsKey(type))
                            {
                                stats[type] = count;
                            }
                        }
                    }
                }

                // Compter les annulations par DateAnnulation dans ce trimestre
                string selectCancellationQuery = @"
            SELECT COUNT(*) as Count
            FROM Licenses
            WHERE Status = 'ملغي'
            AND DateAnnulation IS NOT NULL
            AND CAST(strftime('%m', DateAnnulation) AS INTEGER) >= @StartMonth
            AND CAST(strftime('%m', DateAnnulation) AS INTEGER) <= @EndMonth
            AND strftime('%Y', DateAnnulation) = @Year";

                using (var command = new SQLiteCommand(selectCancellationQuery, connection))
                {
                    command.Parameters.AddWithValue("@StartMonth", currentQuarter.Item1);
                    command.Parameters.AddWithValue("@EndMonth", currentQuarter.Item2);
                    command.Parameters.AddWithValue("@Year", DateTime.Now.Year.ToString());

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        stats["قرار الإلغاء"] = Convert.ToInt32(result);
                    }
                }
            }

            return stats;
        }

        public Dictionary<string, int> GetYearlyStatistics()
        {
            var stats = new Dictionary<string, int>
    {
        { "تصريح", 0 },
        { "رخصة", 0 },
        { "قرار التحويل", 0 }, // ✅ CORRECTION: Maintenir le nom d'affichage
        { "قرار الإلغاء", 0 }
    };

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // ✅ CORRECTION: Ajouter "طلب تحويل" dans la requête
                string selectActiveQuery = @"
            SELECT Type, COUNT(*) as Count
            FROM Licenses
            WHERE Type IN ('تصريح', 'رخصة', 'طلب تحويل')
            AND Status = 'نشط'
            AND strftime('%Y', Date) = @Year
            GROUP BY Type";

                using (var command = new SQLiteCommand(selectActiveQuery, connection))
                {
                    command.Parameters.AddWithValue("@Year", DateTime.Now.Year.ToString());

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["Type"].ToString();
                            int count = Convert.ToInt32(reader["Count"]);

                            // ✅ CORRECTION: Mapper "طلب تحويل" vers "قرار التحويل" pour l'affichage
                            if (type == "طلب تحويل")
                            {
                                stats["قرار التحويل"] = count;
                            }
                            else if (stats.ContainsKey(type))
                            {
                                stats[type] = count;
                            }
                        }
                    }
                }

                // Compter les annulations par DateAnnulation cette année
                string selectCancellationQuery = @"
            SELECT COUNT(*) as Count
            FROM Licenses
            WHERE Status = 'ملغي'
            AND DateAnnulation IS NOT NULL
            AND strftime('%Y', DateAnnulation) = @Year";

                using (var command = new SQLiteCommand(selectCancellationQuery, connection))
                {
                    command.Parameters.AddWithValue("@Year", DateTime.Now.Year.ToString());

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        stats["قرار الإلغاء"] = Convert.ToInt32(result);
                    }
                }
            }

            return stats;
        }


        private (int, int) GetCurrentQuarterMonths()
        {
            int currentMonth = DateTime.Now.Month;

            if (currentMonth >= 1 && currentMonth <= 3)
                return (1, 3);
            else if (currentMonth >= 4 && currentMonth <= 6)
                return (4, 6);
            else if (currentMonth >= 7 && currentMonth <= 9)
                return (7, 9);
            else
                return (10, 12);
        }
    }
}
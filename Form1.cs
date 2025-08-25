using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace LicenseManagement
{
    public partial class Form1 : Form
    {
        TextBox txtType;
        TextBox txtReference;
        DataGridView dgvLicenses;
        Button btnAdd;
        Button btnEdit;
        Button btnDelete;

        public Form1()
        {
            InitializeComponent();
            SetupControls();
            LoadLicenses();
        }

        void SetupControls()
        {
            txtType = new TextBox { Left = 20, Top = 20, Width = 120 };
            txtReference = new TextBox { Left = 150, Top = 20, Width = 120 };
            btnAdd = new Button { Text = "Add", Left = 280, Top = 18 };
            btnEdit = new Button { Text = "Edit", Left = 360, Top = 18 };
            btnDelete = new Button { Text = "Delete", Left = 440, Top = 18 };
            dgvLicenses = new DataGridView
            {
                Left = 20,
                Top = 60,
                Width = 540,
                Height = 300,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            btnAdd.Click += btnAdd_Click;
            btnEdit.Click += btnEdit_Click;
            btnDelete.Click += btnDelete_Click;
            dgvLicenses.SelectionChanged += dgvLicenses_SelectionChanged;

            Controls.Add(txtType);
            Controls.Add(txtReference);
            Controls.Add(btnAdd);
            Controls.Add(btnEdit);
            Controls.Add(btnDelete);
            Controls.Add(dgvLicenses);
        }

        void LoadLicenses()
        {
            using (var cn = Database.GetConnection())
            using (var da = new SQLiteDataAdapter("SELECT Id, Type, Reference, Date FROM Licenses", cn))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvLicenses.DataSource = dt;
            }
        }

        void btnAdd_Click(object sender, EventArgs e)
        {
            using (var cn = Database.GetConnection())
            using (var cmd = new SQLiteCommand("INSERT INTO Licenses(Type, Reference, Date, Status) VALUES(@t,@r,@d,'نشط')", cn))
            {
                cmd.Parameters.AddWithValue("@t", txtType.Text);
                cmd.Parameters.AddWithValue("@r", txtReference.Text);
                cmd.Parameters.AddWithValue("@d", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
            LoadLicenses();
        }

        void btnEdit_Click(object sender, EventArgs e)
        {
            if (dgvLicenses.CurrentRow == null) return;
            int id = Convert.ToInt32(dgvLicenses.CurrentRow.Cells["Id"].Value);
            using (var cn = Database.GetConnection())
            using (var cmd = new SQLiteCommand("UPDATE Licenses SET Type=@t, Reference=@r WHERE Id=@id", cn))
            {
                cmd.Parameters.AddWithValue("@t", txtType.Text);
                cmd.Parameters.AddWithValue("@r", txtReference.Text);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            LoadLicenses();
        }

        void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvLicenses.CurrentRow == null) return;
            int id = Convert.ToInt32(dgvLicenses.CurrentRow.Cells["Id"].Value);
            using (var cn = Database.GetConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM Licenses WHERE Id=@id", cn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            LoadLicenses();
        }

        void dgvLicenses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvLicenses.CurrentRow == null) return;
            txtType.Text = dgvLicenses.CurrentRow.Cells["Type"].Value?.ToString();
            txtReference.Text = dgvLicenses.CurrentRow.Cells["Reference"].Value?.ToString();
        }
    }
}


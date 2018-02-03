using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MdfMaster.Model;
using Microsoft.Win32;
using System.Data;
using System.Data.SqlClient;

namespace MdfMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string ConnStringTemplate = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={0};Integrated Security=True;Connect Timeout=30";

        private void BrowseMdfButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "数据库文件|*.mdf";

            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        #region RunCommand
        private void RunCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(this.FilePathTextBox.Text);
        }

        private void RunCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.CommandTextBox.SelectedText))
            {
                this.ExecuteCommand(this.CommandTextBox.Text);
            }
            else
            {
                this.ExecuteCommand(this.CommandTextBox.SelectedText);
            }
        }
        #endregion

        private void ExecuteCommand(string cmdText)
        {
            this.ResultGrid.RowDefinitions.Clear();
            this.ResultGrid.Children.Clear();

            try
            {
                var ConnString = string.Format(ConnStringTemplate, this.FilePathTextBox.Text);

                using (SqlConnection sqlConn = new SqlConnection(ConnString))
                {
                    sqlConn.Open();

                    var sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConn;
                    sqlCmd.CommandText = cmdText;

                    DataSet set = new DataSet();

                    var adapter = new SqlDataAdapter(sqlCmd);

                    adapter.Fill(set);

                    if (set.Tables.Count == 0)
                    {
                        TextBox box = new TextBox();
                        box.AcceptsTab = true;
                        box.AcceptsReturn = true;
                        box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                        box.Text = string.Format("命令已成功执行{0}{1}", Environment.NewLine, DateTime.Now.ToString());

                        this.ResultGrid.Children.Add(box);
                    }
                    else
                    {
                        for (int i = 0; i < set.Tables.Count; i++)
                        {
                            var table = set.Tables[i];

                            DataGrid grid = new DataGrid();

                            grid.ItemsSource = table.DefaultView;

                            this.ResultGrid.Children.Add(grid);

                            this.ResultGrid.RowDefinitions.Add(new RowDefinition());
                            Grid.SetRow(grid, i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TextBox box = new TextBox();
                box.AcceptsTab = true;
                box.AcceptsReturn = true;
                box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                box.Text = ex.ToString();

                this.ResultGrid.Children.Add(box);
            }
        }

        private void RefreshDatabaseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ConnString = string.Format(ConnStringTemplate, this.FilePathTextBox.Text);

            try
            {
                using (SqlConnection sqlConn = new SqlConnection(ConnString))
                {
                    sqlConn.Open();

                    var sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConn;
                    sqlCmd.CommandText = @"
SELECT * FROM sysobjects WHERE xtype='u' ORDER BY Name

SELECT * FROM sysobjects WHERE xtype='p' ORDER BY Name

SELECT object_name(B.constid) AS FkName,
    object_name(A.parent_obj) AS ForeignTable,
    col_name(A.parent_obj,B.fkey) AS ForeignColumn,
    object_name(B.rkeyid) AS MainTable,
    col_name(B.rkeyid,B.rkey) AS MainColumn
FROM sysobjects A
INNER JOIN sysforeignkeys B on A.id=B.constid
";

                    var reader = sqlCmd.ExecuteReader();

                    List<Model.Table> tables = new List<Model.Table>();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var tableName = reader["Name"].ToString();

                            tables.Add(new Model.Table { Name = tableName });
                        }
                    }
                    TableTree.ItemsSource = tables;

                    List<Model.StoredProcedure> sps = new List<Model.StoredProcedure>();
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var spName = reader["Name"].ToString();

                            sps.Add(new StoredProcedure { Name = spName });
                        }
                    }
                    SpTree.ItemsSource = sps;

                    List<Model.ForeignKeyeEntity> fkEntities = new List<ForeignKeyeEntity>();
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var entity = new ForeignKeyeEntity();

                            entity.FkName = reader["FkName"].ToString();
                            entity.ForeignTable = reader["ForeignTable"].ToString();
                            entity.ForeignColumn = reader["ForeignColumn"].ToString();
                            entity.MainTable = reader["MainTable"].ToString();
                            entity.MainColumn = reader["MainColumn"].ToString();

                            fkEntities.Add(entity);
                        }
                    }
                    FkTree.ItemsSource = fkEntities;
                }
            }
            catch (Exception ex)
            {
                TextBox box = new TextBox();
                box.AcceptsTab = true;
                box.AcceptsReturn = true;
                box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                box.Text = ex.ToString();

                this.ResultGrid.Children.Add(box);
            }
        }

        private void ViewSchemaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tableName = e.Parameter.ToString();

            var cmdText = string.Format(viewSchemaCmdTemplate, tableName);

            ExecuteCommand(cmdText);
        }

        private void Top10Command_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tableName = e.Parameter.ToString();

            var cmdText = string.Format(top10CmdTemplate, tableName);

            ExecuteCommand(cmdText);
        }

        private void ModifySpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.ResultGrid.RowDefinitions.Clear();
            this.ResultGrid.Children.Clear();

            var spName = e.Parameter.ToString();

            var cmdText = string.Format(modifySpCmdTemplate, spName);

            try
            {
                var ConnString = string.Format(ConnStringTemplate, this.FilePathTextBox.Text);

                using (SqlConnection sqlConn = new SqlConnection(ConnString))
                {
                    sqlConn.Open();

                    var sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlConn;
                    sqlCmd.CommandText = cmdText;

                    StringBuilder builder = new StringBuilder();

                    var reader = sqlCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        builder.Append(reader[0].ToString());
                    }

                    this.CommandTextBox.Text = builder.ToString();
                }
            }
            catch (Exception ex)
            {
                TextBox box = new TextBox();

                box.AcceptsTab = true;
                box.AcceptsReturn = true;
                box.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                box.Text = ex.ToString();

                this.ResultGrid.Children.Add(box);
            }
        }

        private void DropSpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var spName = e.Parameter.ToString();

            var cmdText = string.Format(dropSpCmdTeplate, spName);

            ExecuteCommand(cmdText);
        }

        private const string viewSchemaCmdTemplate = @"
SELECT
    col.colorder AS ColOrder,
    col.name AS Name,
    COLUMNPROPERTY(obj.id,col.name,'IsIdentity') AS IsIdentity,
    (case when idxkey.colid = col.colid then 1 else 0 end) IsPrimaryKey,
    tps.name AS ColType,
    col.length AS ColBytes,
    COLUMNPROPERTY(obj.id,col.name,'PRECISION') AS ColLength,
    col.isnullable AS IsNullable,
    isnull(cmt.text,'') AS DefaultValue
FROM syscolumns col
    INNER JOIN sysobjects obj
        ON obj.id = col.id
    INNER JOIN systypes tps
        ON col.xtype=tps.xusertype
    LEFT JOIN sysindexkeys idxkey
        ON obj.id = idxkey.id
    LEFT JOIN syscomments cmt
        ON col.cdefault = cmt.id
WHERE obj.name='{0}'
";

        /// <summary>
        /// 查看前20条记录
        /// </summary>
        private const string top10CmdTemplate = "SELECT TOP 10 * FROM [{0}]";

        /// <summary>
        /// 修改存储过程
        /// </summary>
        private const string modifySpCmdTemplate = "sp_helptext [{0}]";

        /// <summary>
        /// 删除存储过程
        /// </summary>
        private const string dropSpCmdTeplate = "drop procedure [{0}]";

        /// <summary>
        /// 删除外键约束
        /// </summary>
        private const string dropFkCmdTemplate = "alter table [{0}] drop constraint {1}";
    }
}

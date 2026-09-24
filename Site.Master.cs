using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Web.UI.HtmlControls;
using ERP.classes;

namespace ERP
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            con = new SqlConnection(ConnectionString);

            userId = Convert.ToInt32(Session["id"]);
         
           
            if (!IsPostBack)
            {
                LoadUserData();
                LoadUserPermissions();
                LoadAll();
            }
        }

        public static string ConnectionString
        {
            get
            {
                return System.Configuration.ConfigurationManager.ConnectionStrings["Con_DR"].ConnectionString;
            }

        }

        SqlConnection con = new SqlConnection();
        private int userId;

        private void LoadUserData()
        {
            if (Session["id"] != null)
            {
                int userId = Convert.ToInt32(Session["id"]);
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    // استعلام جلب بيانات المستخدم مع الشركة وتاريخ الانضمام (عدل أسماء الأعمدة حسب جدول قاعدة البيانات لديك)
                    string query = "SELECT employee_name, ISNULL(comp_name_ar, N'غير محدد') AS company_name, ISNULL(a_users.register_date, GETDATE()) AS reg_date FROM dbo.a_users left JOIN A_COMPANY ON A_COMPANY.COMP_ID=A_USERS.COMPANY_ID WHERE userid = @UserId";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string userName = reader["employee_name"].ToString();
                                string companyName = reader["company_name"].ToString();
                                string regDate = Convert.ToDateTime(reader["reg_date"]).ToString("yyyy-MM-dd");

                                // 1. عرض الاسم في الشريط العلوي
                                if (displayUserName != null)
                                {
                                    displayUserName.InnerText = userName;
                                }

                                // 2. تعبئة الحقول داخل المودال الموحد
                                if (txtProfileUserName != null)
                                {
                                    txtProfileUserName.Text = userName;
                                }
                                if (modalHeaderUserName != null)
                                {
                                    modalHeaderUserName.InnerText = userName;
                                }
                                if (txtProfileCompanyName != null)
                                {
                                    txtProfileCompanyName.Text = companyName;
                                }
                                if (txtProfileJoinDate != null)
                                {
                                    txtProfileJoinDate.Text = regDate;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void LoadUserPermissions()
        {
            if (Session["id"] != null)
            {
                int userId = Convert.ToInt32(Session["id"]);
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    // استعلام جلب صلاحيات المستخدم (عدل اسم جدول الصلاحيات بحسب هيكل قاعدة البيانات لديك)
                    string query = @"SELECT screen_name AS [الصفحة / النظام]
                                     
                                     FROM dbo.a_permissions WHERE  user_id = @UserId  and screen_status=1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gvUserPermissions.DataSource = dt;
                            gvUserPermissions.DataBind();
                        }
                    }
                }
            }
        }

        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            if (Session["id"] != null)
            {
                int userId = Convert.ToInt32(Session["id"]);
                string currentPass = txtCurrentPass.Text.Trim();
                string newPass = txtNewPass.Text.Trim();
                string confirmPass = txtConfirmPass.Text.Trim();

                if (string.IsNullOrEmpty(currentPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
                {
                    lblPassMessage.Text = "الرجاء تعبئة جميع حقول كلمات المرور.";
                    lblPassMessage.CssClass = "small fw-bold text-danger d-block mb-2";
                    return;
                }

                if (newPass != confirmPass)
                {
                    lblPassMessage.Text = "كلمة المرور الجديدة وتأكيدها غير متطابقتين.";
                    lblPassMessage.CssClass = "small fw-bold text-danger d-block mb-2";
                    return;
                }

                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    // 1. التحقق من كلمة المرور الحالية
                    string checkQuery = "SELECT COUNT(1) FROM dbo.a_users WHERE userid = @UserId AND pass = @CurrentPassword";
                    using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@UserId", userId);
                        cmdCheck.Parameters.AddWithValue("@CurrentPassword", currentPass);
                        int exists = Convert.ToInt32(cmdCheck.ExecuteScalar());

                        if (exists == 0)
                        {
                            lblPassMessage.Text = "كلمة المرور الحالية غير صحيحة.";
                            lblPassMessage.CssClass = "small fw-bold text-danger d-block mb-2";
                            return;
                        }
                    }

                    // 2. تحديث كلمة المرور
                    string updateQuery = "UPDATE dbo.a_users SET pass = @NewPassword WHERE userid = @UserId";
                    using (SqlCommand cmdUpdate = new SqlCommand(updateQuery, con))
                    {
                        cmdUpdate.Parameters.AddWithValue("@NewPassword", newPass);
                        cmdUpdate.Parameters.AddWithValue("@UserId", userId);
                        cmdUpdate.ExecuteNonQuery();

                        lblPassMessage.Text = "تم تغيير كلمة المرور بنجاح.";
                        lblPassMessage.CssClass = "small fw-bold text-success d-block mb-2";

                        txtCurrentPass.Text = "";
                        txtNewPass.Text = "";
                        txtConfirmPass.Text = "";
                    }
                }
            }
            else
            {
                Response.Redirect("~/login.aspx");
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/ERP_eltarzy/login.aspx");
        }

        void LoadAll()
        {
            if (Session["employee_name"] != null)
                displayUserName.InnerHtml = Session["employee_name"].ToString();

            //DataTable dt = new DataTable();
            //string str = "SELECT employee_name FROM employees INNER JOIN Users ON Users.employee_id = employees.employee_id WHERE UserID = @UserId";
            //SqlDataAdapter ad = new SqlDataAdapter(str, con);
            //ad.SelectCommand.Parameters.AddWithValue("@UserId", userId);
            //ad.Fill(dt);

            //if (dt.Rows.Count != 0)
            //{
            //    userName.InnerHtml = dt.Rows[0]["employee_name"].ToString() + "  <i class='mdi mdi-chevron-down'></i>";

                LoadPermissions();
            //}
        }

        private void LoadPermissions()
        {
            string query = @"SELECT screen_id, screen_status FROM a_permissions WHERE user_id = @id";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@id", userId);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTable dt_permissions = new DataTable();
                    dt_permissions.Load(reader);

                    Dictionary<string, HtmlGenericControl> screenIdToElement = new Dictionary<string, HtmlGenericControl>()
                    {

                         { "li201", li201 },
                        { "li202", li202 },
                        { "li203", li203 },
                        { "li204", li204 },
                        { "li205", li205 },
                        { "li206", li206 },
                        { "li207", li207 },
                        { "li208", li208 },
                        { "li209", li209 },
                        { "li111", li111 },
                        { "li112", li112 },
                        { "li113", li113 },
                        { "li114", li114 },
                        { "li115", li115 },
                        { "li116", li116 },
                        { "li117", li117 },
                        { "li118", li118 },
                        { "li119", li119 },
                        { "li1110", li1110 },
                        { "li1111", li1111 },
                        { "li1112", li1112 },
                        { "li1113", li1113 },
                        { "li1114", li1114 },
                        { "li1115", li1115 },
                       
                        { "li121", li121 },
                        { "li122", li122 },
                        { "li123", li123 },
                        { "li124", li124 },
                        { "li125", li125 },
                        { "li126", li126 },
                        { "li127", li127 },

                        { "li131", li131 },
                        { "li132", li132 },
                        { "li133", li133 },
                        { "li134", li134 },
                        { "li135", li135 },
                        { "li136", li136 },
                      
                        { "li141", li141 },
                        { "li142", li142 },
                        { "li143", li143 },
                        { "li144", li144 },
                        { "li145", li145 },
                        { "li146", li146 },
                        { "li147", li147 },
                        { "li148", li148 },
                        { "li149", li149 },

                        { "li151", li151 },
                        { "li152", li152 },
                        { "li153", li153 },
                        { "li154", li154 },
                      
                        { "li161", li161 },
                        { "li162", li162 },

                        { "li171", li171 },
                        { "li172", li172 },
                        { "li173", li173 },
                        { "li174", li174 },
                        { "li175", li175 },
                        { "li176", li176 },
                        { "li177", li177 },
                        { "li178", li178 },
                        { "li179", li179 },
                        { "li1710", li1710 },
                        { "li1711", li1711 },
                        { "li1712", li1712 },

                        { "li181", li181 },
                        { "li182", li182 },

                        { "li191", li191 },
                        { "li192", li192 },

                        { "li1101", li1101 },
                        { "li1102", li1102 },
                        { "li1103", li1103 },
                        { "li1104", li1104 },
                        { "li1105", li1105 },
                        { "li1106", li1106 },
                        { "li1107", li1107 },
                        { "li1108", li1108 },
                        { "li1109", li1109 },
                        { "li11010", li11010 },
                        { "li11011", li11011 },
                        { "li11012", li11012 },
                        { "li11013", li11013 },
                        { "li11014", li11014 },
                        { "li11016", li11016 },
                        { "li11017", li11017 },
                        { "li11018", li11018 },
                        { "li11019", li11019 },

                        { "li1001", li1001 },
                        { "li1002", li1002 },
                        { "li1003", li1003 },
                        { "li1004", li1004 },
                        { "li1005", li1005 },
                        { "li1006", li1006 },
                        { "li1007", li1007 },
                        { "li1008", li1008 },
                        { "li1201", li1201 },
                        { "li1202", li1202 },
                        { "li1203", li1203 },
                        { "li1204", li1204 },
                        { "li1205", li1205 },
                        { "li11001", li11001 },
                        { "li11002", li11002 },
                        

                    };


                    foreach (DataRow row in dt_permissions.Rows)
                    {
                        string screenId = row["screen_id"].ToString();
                        bool isVisible = Convert.ToBoolean(row["screen_status"]);

                        if (screenIdToElement.ContainsKey(screenId))
                        {
                            screenIdToElement[screenId].Visible = isVisible;
                        }
                    }

                    HideParentElements();
                }
            }
        }

        private void HideParentElements()
        {
            Dictionary<HtmlGenericControl, List<HtmlGenericControl>> parentToChildElements = new Dictionary<HtmlGenericControl, List<HtmlGenericControl>>()
            {
                 { li200, new List<HtmlGenericControl> { li201, li202, li203, li204, li205, li206, li207, li208, li209 } },
            
                { li110, new List<HtmlGenericControl> { li111, li114, li115, li116, li117, li118, li119, li1110, li1111, li1112, li1113, li1114, li1115 } },
            
                { li120, new List<HtmlGenericControl> { li121, li122, li123, li124, li125, li126, li127 } },
                { li130, new List<HtmlGenericControl> { li131, li132, li133, li134, li135, li136 } },
                { li140, new List<HtmlGenericControl> { li141, li142, li143, li144, li145, li146, li147, li148, li149 } },
                { li150, new List<HtmlGenericControl> { li151, li153, li154 } },
                { li160, new List<HtmlGenericControl> { li161, li162 } },
                { li170, new List<HtmlGenericControl> { li171, li172, li173, li174, li175, li176, li177, li178, li179, li1710, li1711, li1712 } },
                { li180, new List<HtmlGenericControl> { li181, li182 } },
                { li190, new List<HtmlGenericControl> { li191, li192 } },
                { li1100, new List<HtmlGenericControl> {  li1101, li1102, li1103, li1105, li1106, li1107, li1108, li1109, li11010, li11011, li11012, li11013, li11014, li11016, li11017, li11018, li11019 } },
                { li11000, new List<HtmlGenericControl> { li11001, li11002 } },
                 { li1000, new List<HtmlGenericControl> { li1001, li1002,li1003,li1004,li1005,li1006,li1007,li1008 } },
                 { li1200, new List<HtmlGenericControl> { li1201, li1202,li1203,li1204,li1205 ,li112,li1004,li113} },
            };

            foreach (var entry in parentToChildElements)
            {
                HtmlGenericControl parentElement = entry.Key;
                List<HtmlGenericControl> childElements = entry.Value;

                bool allChildHidden = true;
                foreach (HtmlGenericControl childElement in childElements)
                {
                    if (childElement.Visible)
                    {
                        allChildHidden = false;
                        break;
                    }
                }

                parentElement.Visible = !allChildHidden;
            }
        }

        
    }
}

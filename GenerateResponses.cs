using System;
using System.Configuration;
using System.Collections;
using System.Text;
using System.IO;
using System.Xml;    
using System.Data;
using System.Data.SqlClient;

public class GenerateResponses
{

	public static void ExecuteSql(string sql){
		
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
		using(oConn)
		{ 	
			SqlCommand oCmd = new SqlCommand(sql, oConn);
			oCmd.CommandType = CommandType.Text;
			oConn.Open();
			oCmd.ExecuteNonQuery();
		}
	}

	public static void LoadMCATAnswers()
	{
		XmlDocument doc = new XmlDocument();
		doc.Load("Ortho_Bootstrap_Parameters.xml");
		MSS.Engines.BaseEngineGRM engine = new MSS.Engines.BaseEngineGRM("Fischer Distribution", doc); // create engine


		engine.loadItems(doc);
		string sql = "SELECT CaseID,[simulation_Theta 0],[simulation_Theta 1],[simulation_Theta 2] FROM dbo.Responses WHERE ItemSelected ='{0}'" ;
		string sql2 = "UPDATE dbo.Responses SET CategorySelected ={0} WHERE [simulation_Theta 0] = {1} and [simulation_Theta 1]= {2} and [simulation_Theta 2]= {3} and ItemSelected = '{4}' and CaseID = '{5}'" ;
		
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
        SqlDataReader oReader;
  
		XmlNodeList Items = doc.GetElementsByTagName("Item");	
		for (int i = 0; i < Items.Count; i++){  // trial counter    

				SqlCommand oCmd = new SqlCommand(String.Format(sql, Items[i].Attributes["VariableName"].Value), oConn);
				
				Console.WriteLine( String.Format(sql, Items[i].Attributes["VariableName"].Value) );
				oCmd.CommandType = CommandType.Text;
				oConn.Open();
				oReader = oCmd.ExecuteReader();
				
				while (oReader.Read())
				{ 
					double[] _simulated_Theta = new double[3];
					_simulated_Theta[2] = oReader.GetDouble(3);
					_simulated_Theta[1] = oReader.GetDouble(2); 
					_simulated_Theta[0] = oReader.GetDouble(1);
					
					int selectedCategory = engine.SelectResponse(Items[i].Attributes["VariableName"].Value, _simulated_Theta);
					string sqlUpdate = String.Format(sql2, selectedCategory.ToString(), oReader.GetDouble(1).ToString(), oReader.GetDouble(2).ToString(), oReader.GetDouble(3).ToString(), Items[i].Attributes["VariableName"].Value,  oReader.GetInt32(0).ToString());
					ExecuteSql(sqlUpdate);
				}
				oReader.Close();
				oConn.Close();    
		}
			
		return;
	}
	
	public static void Main(string[] args)
	{
		//LoadMCATAnswers();
		
		XmlDocument doc = new XmlDocument();
		doc.Load("Ortho_Full_Bank_Parameters.xml");


		// MSS.Engines.MCATEngineGRM mcat1 = new MSS.Engines.MCATEngineGRM("Fischer Distribution", doc, "DomainReduction");
		// RunMCAT(mcat1, "dbo.MCAT_Clinical_Fischer_DomainReduction2", "Fischer Distribution", "clinical");
	 
		// MSS.Engines.MCATEngineGRM mcat2 = new MSS.Engines.MCATEngineGRM("Fischer Distribution", doc, "DomainReduction");
		// RunMCAT(mcat2, "dbo.MCAT_Simulation_Fischer_DomainReduction", "Fischer Distribution", "simulation"); 
		 
		// MSS.Engines.MCATEngineGRM mcat3 = new MSS.Engines.MCATEngineGRM("Fischer Distribution", doc, "");
		// RunMCAT(mcat3, "dbo.MCAT_Clinical_Fischer", "Fischer Distribution", "clinical"); 
		
		// MSS.Engines.MCATEngineGRM mcat4 = new MSS.Engines.MCATEngineGRM("Fischer Distribution", doc, "");
		// RunMCAT(mcat4, "dbo.MCAT_Simulation_Fischer", "Fischer Distribution", "simulation");

		// MSS.Engines.MCATEngineGRM5 mcat5 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "DomainReduction");
		// RunMCAT(mcat5, "dbo.MCAT_Clinical_Variance_DomainReduction2", "Expected Variance", "clinical");
		 
		// MSS.Engines.MCATEngineGRM5 mcat6 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "DomainReduction");
		// RunMCAT(mcat6, "dbo.MCAT_Simulation_Variance_DomainReduction2", "Expected Variance", "simulation"); 
		 
		// MSS.Engines.MCATEngineGRM5 mcat7 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "");
		// RunMCAT(mcat7, "dbo.MCAT_Clinical_Variance", "Expected Variance", "clinical"); 
		
		// MSS.Engines.MCATEngineGRM5 mcat8 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "");
		// RunMCAT(mcat8, "dbo.MCAT_Simulation_Variance", "Expected Variance", "simulation");

		MSS.Engines.MCATEngineGRM5 mcat9 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "DomainReduction");
		RunMCAT(mcat9, "dbo.MCAT_Simulation_Variance_DomainReduction2_4", "Expected Variance", "simulation");

		MSS.Engines.MCATEngineGRM5 mcat10 = new MSS.Engines.MCATEngineGRM5("Expected Variance", doc, "DomainReduction");
		RunMCAT(mcat10, "dbo.MCAT_Clinical_Variance_DomainReduction2_4", "Expected Variance", "clinical");	
		return;
	}
	
	public static void RunMCAT(MSS.Engines.BaseEngineGRM mcat, string db, string SelectionMethod, string sample)
	{
		ArrayList Trials = null;
		if (sample == "clinical"){
			Trials  =  GetClinicalTrials();
		}
		if (sample == "simulation"){
			Trials  =  GetSimulationTrials();
		}		
		
		string sql = "INSERT INTO "+ db + "(caseid,Position,ItemSelected,CategorySelected,[Est. Theta 0],[Est. Theta 1],[Est. Theta 2],[StandardError 0],[StandardError 1],[StandardError 2],Responsetime, Variance, SelectionMethod) Values('{0}',{1},'{2}',{3},{4},{5},{6},{7},{8},{9},{10},{11},'{12}')";
		for (int i = 0; i < Trials.Count; i++){  // trial counter		
			string CaseID = null;
			double theta0 = 0D;
			double theta1 = 0D;
			double theta2 = 0D;			

			if (sample == "clinical"){
				CaseID =  ((Trial2)Trials[i]).CaseID;
			
				theta0 = ((Trial2)Trials[i]).Theta0;
				theta1 = ((Trial2)Trials[i]).Theta1;			
				theta2 = ((Trial2)Trials[i]).Theta2;
			}
			if (sample == "simulation"){
				CaseID =  ((Trial)Trials[i]).CaseID;
			}			

			Console.WriteLine("Processing " + CaseID);
			mcat.initializeTest();
			mcat.finished = false;
			double responsetime;

			int j = 0;
			while(!mcat.finished){
			//for (int j = 0; j < mcat.itemCount ; j++){
				DateTime start = DateTime.Now;
				string nextItem = mcat.getCurrentItem(j);

				if(mcat.finished){break;}
				if(mcat.message != string.Empty){
/*
FileStream fs = new FileStream(@"C:\Simulation_2019_05_24\limits\" + sample + "_" + CaseID + "_" + (j).ToString() + ".txt", FileMode.OpenOrCreate, FileAccess.Write);
StreamWriter sw = new StreamWriter(fs,Encoding.UTF8);
sw.WriteLine(mcat.message);
sw.Flush();
sw.Close();
fs.Close();
					mcat.message = string.Empty;
*/				
				}

				int selectedCategory = -1;
				
				if (sample == "clinical"){
					selectedCategory = AnswerClinicalQuestion(nextItem,CaseID);
				}
				if (sample == "simulation"){
					selectedCategory = AnswerSimulatedQuestion(nextItem,CaseID);
				}

				mcat.updateNode(nextItem, selectedCategory);

				responsetime = LogTime(start);
				string sql_Values = String.Format(sql,CaseID,j,nextItem,selectedCategory.ToString(),Math.Round(mcat.Theta[0],3),Math.Round(mcat.Theta[1],3),Math.Round(mcat.Theta[2],3),Math.Round(mcat.StdError[0],3),Math.Round(mcat.StdError[1],3),Math.Round(mcat.StdError[2],3),responsetime,Math.Round(mcat.Variance,3), SelectionMethod) ;
				ExecuteSql( sql_Values );
				j = j + 1;
			}	
			Console.WriteLine("DB: " + db + " Trial: " + CaseID + " : (" + Math.Round(mcat.Theta[0],3).ToString() + "," + Math.Round(mcat.Theta[1],3).ToString() + "," + Math.Round(mcat.Theta[2],3) + ")" );
			
		}
		
		return;
	}
	
	public static double LogTime(DateTime start)
    {
		DateTime stop = DateTime.Now;
		long elapsedTicks = stop.Ticks - start.Ticks;
		TimeSpan interval = new TimeSpan(elapsedTicks);
		return interval.TotalMilliseconds;
    } 
    
 	public static int AnswerClinicalQuestion(string nextItem, string caseID)
	{
		int rtn = -1;
		string sql = "SELECT CategorySelected FROM dbo.Responses WHERE caseid = {1} and ItemSelected = '{0}'";
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
        SqlDataReader oReader;
		SqlCommand oCmd = new SqlCommand(String.Format(sql,nextItem,caseID), oConn);
		oCmd.CommandType = CommandType.Text;
		oConn.Open();
		oReader = oCmd.ExecuteReader();
		
		if (oReader.Read())
		{ 
			rtn = oReader.GetInt32(0); // - 1 ;
		}
		oReader.Close();
		oConn.Close(); 
		
		if(rtn == -1){
			throw new Exception("Can not find response for : " + sql);
		}

		return rtn; 	
	}

	public static int AnswerSimulatedQuestion(string nextItem, string caseID)
	{
		int rtn = -1;
		string sql = "SELECT [\"{0}\"] FROM [dbo].[imputed_05_09] WHERE [\"CaseID\"] ='{1}'";
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
        SqlDataReader oReader;
		SqlCommand oCmd = new SqlCommand(String.Format(sql,nextItem,caseID), oConn);
		oCmd.CommandType = CommandType.Text;
		oConn.Open();
		oReader = oCmd.ExecuteReader();
		
		if (oReader.Read())
		{ 
			rtn = Int32.Parse(oReader.GetString(0)) - 1;
		}
		oReader.Close();
		oConn.Close(); 
		
		if(rtn == -1){
			throw new Exception("Can not find response for : " + sql);
		}
		
		return rtn; 	
	}
	
	public static ArrayList GetClinicalTrials()
	{
	
		ArrayList rtn = new ArrayList();
Console.WriteLine(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);		
		string sqlTrials ="SELECT DISTINCT CaseID, [simulation_Theta 0],[simulation_Theta 1],[simulation_Theta 2] FROM dbo.Responses ORDER BY caseID";
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
//Console.WriteLine("End sql");
        	SqlDataReader oReader;
		SqlCommand oCmd = new SqlCommand(sqlTrials, oConn);
		oCmd.CommandType = CommandType.Text;
		oConn.Open();
		oReader = oCmd.ExecuteReader();
		
		while (oReader.Read())
		{ 
			rtn.Add( new Trial2( oReader.GetInt32(0).ToString(), oReader.GetDouble(1), oReader.GetDouble(2), oReader.GetDouble(3)  ) );
		}
		oReader.Close();
		oConn.Close(); 
		return rtn;
	}	
	
	public static ArrayList GetSimulationTrials()
	{	
		ArrayList rtn = new ArrayList();
		
		string sqlTrials ="SELECT CaseID FROM dbo.CaseIDs_05_09 order by PKID";
		SqlConnection oConn = new SqlConnection(ConfigurationManager.ConnectionStrings["Simulation"].ConnectionString);
        SqlDataReader oReader;
		SqlCommand oCmd = new SqlCommand(sqlTrials, oConn);
		oCmd.CommandType = CommandType.Text;
		oConn.Open();
		oReader = oCmd.ExecuteReader();
		
		while (oReader.Read())
		{ 
			rtn.Add( new Trial( oReader.GetString(0)) );
		}
		oReader.Close();
		oConn.Close(); 
		return rtn;
	}	
}

[Serializable]
public struct Trial{
	public string CaseID;
	public Trial(string _CaseID){
		CaseID = _CaseID;
	}
} 

[Serializable]
public struct Trial2{
	public string CaseID;
	public double Theta0;
	public double Theta1;
	public double Theta2;
	
	public Trial2(string _CaseID, double _Theta0, double _Theta1, double _Theta2){
		CaseID = _CaseID;
		Theta0 = _Theta0;
		Theta1 = _Theta1;
		Theta2 = _Theta2;
	}
}
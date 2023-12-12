using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;

namespace MSS.Engines
{
    public class BaseEngineGRM
    {
        private string _selectionMethod; 
        public string selectionMethod
        {
            get { return _selectionMethod; }
            set{ _selectionMethod = value;}
        } 

        public int itemCount
        {
            get { return _NumTotalItems; }
            set{ _NumTotalItems = value;}
        }  
 		protected static double[,] _CoVariance = new double[3,3];
        protected static double[,] _CoVarianceZero = new double[3,3];
        protected static double _LogisticScaling  = 1.0D; 
        protected static int _MaxCategories;
        protected static double _ThetaIncrement;
        protected static double _MinTheta;
        protected static double _MaxTheta;        
        protected static Hashtable _Items = new Hashtable();
        protected static Hashtable _Domains = new Hashtable();
        protected static Hashtable _ItemIDs  = new Hashtable();
  		protected static double[] _Difficulty;  
        protected static double[][] _CategoryBoundaryValues;
        protected static double[,] _DiscriminationValues;
        protected static int[] _NumCategoriesByItem;
        protected static int[] _ItemsAvailable;
        protected static int _NumTotalItems;
  
	protected static int MAX_LENGTH = 3;

  		protected static double[,,,,] _Probability;
        //protected static double[,,,] CumulativeCategory;
        protected static Hashtable CumulativeCategoryCollection  = new Hashtable();
        
        public bool init = false;
	public bool finished = false;
	public string message = string.Empty;
	protected int _DomainCount0;
	protected int _DomainCount1;
	protected int _DomainCount2;

        public BaseEngineGRM(String itemSelectionMethod, XmlDocument doc)
        {
			_selectionMethod = itemSelectionMethod;

			if(!init){
				loadItems(doc);
				InitializeQuadrature(); // depends of loadItems
				PrepareProbability();
				_CoVarianceZero[0,0] = 1.0;
				_CoVarianceZero[0,1] = 0.00;
				_CoVarianceZero[0,2] = 0.00;
				_CoVarianceZero[1,0] = 0.00;
				_CoVarianceZero[1,1] = 1.0;
				_CoVarianceZero[1,2] = 0.00;
				_CoVarianceZero[2,0] = 0.00;
				_CoVarianceZero[2,1] = 0.00;
				_CoVarianceZero[2,2] = 1.0;

				//Population 1	
				_CoVariance[0,0] = 1.0;
				_CoVariance[0,1] = -0.78; 
				_CoVariance[0,2] = -0.40; //PF-Depress = -0.40
				_CoVariance[1,0] = -0.78; //PF-PainIN = -0.78
				_CoVariance[1,1] = 1.0;
				_CoVariance[1,2] = 0.54; //PainIN-Depress = 0.54
				_CoVariance[2,0] = -0.40; //PF-Depress = -0.40
				_CoVariance[2,1] = 0.54; //PainIN-Depress = 0.54
				_CoVariance[2,2] = 1.0;			
				init = true;
			}
			//Population 2
			/*
			_CoVariance[0,0] = 1.0;
			_CoVariance[0,1] = 0.88;
			_CoVariance[0,2] = 0.81;
			_CoVariance[1,0] = 0.88;
			_CoVariance[1,1] = 1.0;
			_CoVariance[1,2] = 0.81;
			_CoVariance[2,0] = 0.81;
			_CoVariance[2,1] = 0.81;
			_CoVariance[2,2] = 1.0;			
			*/

        }
        
        protected double[] _StdError; 
        public double[] StdError
        {
            get { return _StdError; }
            set{ _StdError = value;}
        } 
        protected double[] _Theta;
        public double[] Theta
        {
            get { return _Theta; }
            set{ _Theta = value;}
        }
        
        protected double _Variance;
        public double Variance
        {
            get { return _Variance; }
            set{ _Variance = value;}
        }
 
        protected ArrayList _ItemsAsked  = new ArrayList();
        protected ArrayList _Responses  = new ArrayList();
   

        protected double[,,] _LikelihoodEstimate;
        protected double[,,] _PosteriorProbability;  

 
		public void loadItems(XmlDocument FormParams){

				_ItemIDs.Clear();
				_Items.Clear();
				_Domains.Clear();


			XmlNodeList Properties  = FormParams.GetElementsByTagName("Property");
			for (int i = 0; i < Properties.Count; i++)
            {
				if(Properties[i].Attributes["Name"].Value == "ThetaIncrement"){
					_ThetaIncrement = Convert.ToDouble(Properties[i].Attributes["Value"].Value);	
				}
				if(Properties[i].Attributes["Name"].Value == "MinTheta"){
					_MinTheta = Convert.ToDouble(Properties[i].Attributes["Value"].Value);	
				}
				if(Properties[i].Attributes["Name"].Value == "MaxTheta"){
					_MaxTheta = Convert.ToDouble(Properties[i].Attributes["Value"].Value);	
				}
            }
			
			XmlNodeList CATitems;

			ArrayList arItemsAvailable = new ArrayList();
			ArrayList arNumCatByItem = new ArrayList();
			ArrayList arDisc = new ArrayList();
			ArrayList arDomain = new ArrayList();
			List<double> liDiff = new List<double>();
			ArrayList arCBV = new ArrayList();
			ArrayList arItemCBV;
			ArrayList arNumCat = new ArrayList();
			
			int j;
			//Get item CAT parameters

			CATitems = FormParams.SelectNodes("descendant::Item");	
	
			Console.WriteLine("Itemcount: " + CATitems.Count.ToString());
				
			//int itemcount = 0;
			for (int i = 0; i < CATitems.Count; i++)
			{
				XmlNode CATitem2 = CATitems[i];
	
				_ItemIDs.Add(CATitems[i].Attributes["VariableName"].Value.ToUpper(), i);
				_Items.Add(i, CATitems[i].Attributes["VariableName"].Value.ToUpper());
				_Domains.Add(CATitems[i].Attributes["VariableName"].Value.ToUpper(), CATitems[i].Attributes["Domain"].Value.ToUpper());
				
				arItemsAvailable.Add(1);

				 //Item Discrimination value (slope or A)
				arDisc.Add(Convert.ToDouble(CATitem2.Attributes["A_GRM"].Value));
				arDomain.Add(Convert.ToInt32(CATitem2.Attributes["Domain"].Value));
				//Category boundary values
				arItemCBV = new ArrayList();
				//Position loading of Threshold based on Value
				//Assumes that the category boundary values are always in the correct order in the XML document
				j = 1;
				foreach (XmlNode n in CATitem2.SelectNodes("Map"))
				{
					//Note: j = number of categories, or one less than the number of category boundary values
					int desc;
					//Check for non-integer step order (might happen with skip or invalid responses)
					if (int.TryParse(n.Attributes["StepOrder"].Value, out desc))
					{
						//This takes into account collapsed categories
						arItemCBV.Add(Convert.ToDouble(n.Attributes["Value"].Value));
						j++;
					}
				}
				arCBV.Add((double[])arItemCBV.ToArray(typeof(double)));
				//Number of categories is one more than the number of category boundary values: ending value from FOR loop
				arNumCat.Add(j);
				if (j > _MaxCategories)
				{
					_MaxCategories = j;
				}
			}

			_DiscriminationValues = new double[arDisc.Count,3];
			for( int k=0; k < arDisc.Count; k++){
			
				if((Int32)arDomain[k] == 0){
				_DiscriminationValues[k,0] = (double) arDisc[k];
				_DiscriminationValues[k,1] = 0.0;
				_DiscriminationValues[k,2] = 0.0;
				}
				if((Int32)arDomain[k] == 1){
				_DiscriminationValues[k,0] = 0.0;
				_DiscriminationValues[k,1] =(double) arDisc[k];
				_DiscriminationValues[k,2] = 0.0;
				}
				if((Int32)arDomain[k] == 2){
				_DiscriminationValues[k,0] = 0.0;
				_DiscriminationValues[k,1] = 0.0;
				_DiscriminationValues[k,2] =(double) arDisc[k];
				}
			}
			
			_Difficulty = liDiff.ToArray();
			_CategoryBoundaryValues = (double[][])arCBV.ToArray(typeof(double[]));
			_NumCategoriesByItem = (int[])arNumCat.ToArray(typeof(int));
			_ItemsAvailable = (int[])arItemsAvailable.ToArray(typeof(int));
			_NumTotalItems = _ItemIDs.Count;

		}
		
        private void PrepareProbability()
        {
            int qpnt;
            int qpnt2;
            int qpnt3;
            int item;
            int cat;

            _Probability = new double[_NumQuadraturePoints, _NumQuadraturePoints,_NumQuadraturePoints, _NumTotalItems, _MaxCategories];

            for (item = 0; item < _NumTotalItems; item++)
            {        
                double[,,,] CumulativeCategory = new double[_NumQuadraturePoints,_NumQuadraturePoints,_NumQuadraturePoints,_NumCategoriesByItem[item] + 1];
                for (qpnt = 0; qpnt < CumulativeCategory.GetLength(0); qpnt++){
                for (qpnt2 = 0; qpnt2 < CumulativeCategory.GetLength(1); qpnt2++){
                for (qpnt3 = 0; qpnt3 < CumulativeCategory.GetLength(2); qpnt3++){
                    CumulativeCategory[qpnt,qpnt2,qpnt3, 0] = 1;
                    CumulativeCategory[qpnt,qpnt2,qpnt3,_NumCategoriesByItem[item]] = 0; //already zero by initialization
                }
                }
                }
                
                for (cat = 0; cat < _NumCategoriesByItem[item] - 1; cat++)
                {
                    for (qpnt = 0; qpnt < CumulativeCategory.GetLength(0); qpnt++){ 
                    for (qpnt2 = 0; qpnt2 < CumulativeCategory.GetLength(1); qpnt2++){               
                    for (qpnt3 = 0; qpnt3 < CumulativeCategory.GetLength(2); qpnt3++){        
                       Point p = (Point)_QuadraturePoints[qpnt, qpnt2, qpnt3];
                       double [] locationvector = new double[3];
                       locationvector[0] = p.x - _CategoryBoundaryValues[item][cat];
                       locationvector[1] = p.y - _CategoryBoundaryValues[item][cat];  
                       locationvector[2] = p.z - _CategoryBoundaryValues[item][cat];
                       double sumvector = (_DiscriminationValues[item,0] * locationvector[0] + _DiscriminationValues[item,1] * locationvector[1] + _DiscriminationValues[item,2] * locationvector[2]); 
                       CumulativeCategory[qpnt,qpnt2,qpnt3, cat + 1] = 1 / (1 + Math.Exp((-1) * _LogisticScaling * sumvector));
					}
                    }
                    }
                }

                for (qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
                for (qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
                for (qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){   
                    _Probability[qpnt,qpnt2,qpnt3, item, 0] = 1 - CumulativeCategory[qpnt,qpnt2,qpnt3, 0];
                    _Probability[qpnt,qpnt2,qpnt3, item, _NumCategoriesByItem[item] - 1] = CumulativeCategory[qpnt,qpnt2,qpnt3, _NumCategoriesByItem[item] - 1];                  
                }
				}
				}

                for (cat = 0; cat < _NumCategoriesByItem[item]; cat++)
                {
                    for (qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
                    for (qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
                    for (qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){
                        _Probability[qpnt,qpnt2,qpnt3, item, cat] = CumulativeCategory[qpnt,qpnt2,qpnt3, cat] - CumulativeCategory[qpnt,qpnt2,qpnt3, cat + 1];
					}
                    }
                    }
                }
                
                CumulativeCategoryCollection[item] = CumulativeCategory;
                
            }

        }

        public virtual void initializeTest(){}
        public virtual string getCurrentItem(int k){ return null;}
        
        public virtual void updateNode(string FormItemOID, int ResponseIndex)
        {

			if (Int32.Parse( _Domains[FormItemOID].ToString() ) == 0){
				this._DomainCount0 = this._DomainCount0 + 1;
			}
			if (Int32.Parse( _Domains[FormItemOID].ToString() ) == 1){
				this._DomainCount1 = this._DomainCount1 + 1;
			}
			if (Int32.Parse( _Domains[FormItemOID].ToString() ) == 2){
				this._DomainCount2 = this._DomainCount2 + 1;
			}
// Console.WriteLine("score for item " + FormItemOID +  " domain " +  _Domains[FormItemOID].ToString());
			int ItemIndex;
			int NumAnsweredItems;

			ItemIndex = (int)_ItemIDs[FormItemOID];
			_ItemsAsked.Add(ItemIndex);
			//Note: this is the collapsed value if category is collapsed; -1 if skipped or invalid
			_Responses.Add(ResponseIndex);
			_ItemsAvailable[ItemIndex] = 0;
			NumAnsweredItems = _ItemsAsked.Count;

			//estimate new theta
			CalcThetaEstimate( _ItemsAsked.Count );

		if(this._DomainCount0 > MAX_LENGTH && this._DomainCount1 > MAX_LENGTH && this._DomainCount2 > MAX_LENGTH ){
			finished = true;
		}
        }
     
        protected void InitializeLikelihood()
        {          
            if (_NumQuadraturePoints == 0)
            {
				return;
            }
 
            _LikelihoodEstimate = new double[_NumQuadraturePoints, _NumQuadraturePoints, _NumQuadraturePoints];
            _PosteriorProbability = new double[_NumQuadraturePoints, _NumQuadraturePoints, _NumQuadraturePoints];
           
            Point estPoint = new Point(_Theta[0], _Theta[1], _Theta[2]);
            
            for (int i = 0; i < _NumQuadraturePoints; i++){
			for (int j = 0; j < _NumQuadraturePoints; j++){
			for (int k = 0; k < _NumQuadraturePoints; k++){			
				_LikelihoodEstimate[i,j,k] = _PriorDistribution[i,j,k]; 
				_PosteriorProbability[i,j,k] = _PriorDistribution[i,j,k];           
			}
			}
			}

        }		
        protected double[,,] _PriorDistribution;
        protected double _PriorDistributionMean = 0D;
        protected double _PriorDistributionStdDev = 1D;
        
        protected double[,,] SetNormalDistribution(Point[,,] DataArray, double Mean, double StdDev)
        {
            if (DataArray == null) {
                return null;
            }
            if (Mean.Equals(null)) {
                Mean = 0.0;
            }
            if (StdDev.Equals(null)) {
                StdDev = 1.0;
            }
            else if (StdDev <= 0.0) {
                StdDev = 1.0;
            }
            
            double[,,] ar1d = new double[DataArray.GetLength(0), DataArray.GetLength(1), DataArray.GetLength(2)];
            double tmp;

            double detMatrix  =  Determinant33(_CoVarianceZero);
            double sum = 0.0D;
  
            double[,] inverse =  Inverse33(_CoVariance);

			for (int i = 0; i < DataArray.GetLength(0); i++) {
			for (int j = 0; j < DataArray.GetLength(1); j++) {
			for (int k = 0; k < DataArray.GetLength(2); k++) {
				Point theta = DataArray[i,j,k];
				double x = theta.x;
				double y = theta.y;	
				double z = theta.z;	
				tmp = -1*(x*x*inverse[0,0] + x*y*inverse[0,1] + x*z*inverse[0,2] + y*x*inverse[1,0]+ y*y*inverse[1,1]+ y*z*inverse[1,2]+ z*x*inverse[2,0]+ z*y*inverse[2,1]+ z*z*inverse[2,2])/2;
				ar1d[i,j,k] = Math.Pow((2 * Math.PI),-3/2)*Math.Pow(detMatrix,-.5) * Math.Exp(tmp) ;
				sum = sum + ar1d[i,j,k];
			}
			}
			}
            return ar1d;
        }
        

        protected static int _NumQuadraturePoints;
        protected static Point[,,] _QuadraturePoints;
        protected static void InitializeQuadrature()
        {
            int i;
            //Calculate quadrature points
            ArrayList Points = new ArrayList();
            double pnt = _MinTheta;

            //Assume 20 decimal places
            double Pow10;
            i = 1;
            double OnePercent = _ThetaIncrement * 0.01;
            do
            {
                i--;
                Pow10 = Math.Pow(10, i);
            } while ((i > -20) && (Pow10 > OnePercent));
            int RoundingPlace = -1 * i;


            do
            {
                Points.Add(pnt);
                pnt = Math.Round((pnt + _ThetaIncrement), RoundingPlace);
            } while (pnt <= _MaxTheta);

            //Calculate number of quadrature points
            _NumQuadraturePoints = Points.Count;
            _QuadraturePoints  = new Point[ _NumQuadraturePoints, _NumQuadraturePoints, _NumQuadraturePoints];
	
			for(int k = 0; k < _NumQuadraturePoints; k++){
			for(int j = 0; j < _NumQuadraturePoints; j++){
			for(int l = 0; l < _NumQuadraturePoints; l++){
				Point p = new Point((double)Points[k], (double)Points[j], (double)Points[l]);
				_QuadraturePoints[k,j,l] = p;
			} 
			}
			}
        }
         
        protected void CalcThetaEstimate(int itemID)
        {

    //FileStream fs = new FileStream(@"C:\code\Simulation_2019_02_20\results\" + (itemID - 1).ToString() + ".txt", FileMode.OpenOrCreate, FileAccess.Write);
    //StreamWriter sw = new StreamWriter(fs,Encoding.UTF8);
    
            double[,,] QAProbability = new double[_QuadraturePoints.GetLength(0), _QuadraturePoints.GetLength(1), _QuadraturePoints.GetLength(2)];
            
			for (int i = 0; i < _Probability.GetLength(0); i++) {
			for (int j = 0; j < _Probability.GetLength(1); j++) {
			for (int k = 0; k < _Probability.GetLength(2); k++) {
				QAProbability[i,j,k] = _Probability[i,j,k, (int)_ItemsAsked[_ItemsAsked.Count - 1], (int)_Responses[_Responses.Count - 1]];
				
				//if(QAProbability[i,j,k] == 0D){
				//	QAProbability[i,j,k] = Double.Epsilon;
				//}
				
			}
			}  
			}  
			
			for (int i = 0; i < _LikelihoodEstimate.GetLength(0); i++) {
			for (int j = 0; j < _LikelihoodEstimate.GetLength(1); j++) {
			for (int k = 0; k < _LikelihoodEstimate.GetLength(2); k++) {
			
				//sw.WriteLine( "(" + i.ToString() + "," + j.ToString() + "," + k.ToString() + ")\t\t" + _LikelihoodEstimate[i,j,k].ToString() + "\t\t" +  QAProbability[i,j,k].ToString() + "\t\t" + (_LikelihoodEstimate[i,j,k] * QAProbability[i,j,k]).ToString()  );
				
				
				_LikelihoodEstimate[i,j,k] = _LikelihoodEstimate[i,j,k] * QAProbability[i,j,k];	
				
				//if(_LikelihoodEstimate[i,j,k] == 0D){
				//	_LikelihoodEstimate[i,j,k] = Double.Epsilon;
				//}
			}
			}
			}

   //sw.Flush();
   // sw.Close();
   // fs.Close();
	
			double[,,] h = new double[_QuadraturePoints.GetLength(0), _QuadraturePoints.GetLength(1), _QuadraturePoints.GetLength(2)];
			double  denominatorH = ArraySum(_LikelihoodEstimate);

			for (int i = 0; i < _LikelihoodEstimate.GetLength(0); i++) {
			for (int j = 0; j < _LikelihoodEstimate.GetLength(1); j++) {
			for (int k = 0; k < _LikelihoodEstimate.GetLength(2); k++) {
				h[i,j,k] = _LikelihoodEstimate[i,j,k] / denominatorH;
			}
			}
			}
	
			_PosteriorProbability = h;
			
			_Theta[0] = 0D;
			_Theta[1] = 0D;
			_Theta[2] = 0D;
	
			for (int i = 0; i < _LikelihoodEstimate.GetLength(0); i++) {
            for (int j = 0; j < _LikelihoodEstimate.GetLength(1); j++) {
            for (int k = 0; k < _LikelihoodEstimate.GetLength(2); k++) {
				_Theta[0] = _Theta[0] +  ((Point)_QuadraturePoints[i,0,0]).x  * h[i,j,k] ;
				_Theta[1] = _Theta[1] +  ((Point)_QuadraturePoints[0,j,0]).y  * h[i,j,k] ;
				_Theta[2] = _Theta[2] +  ((Point)_QuadraturePoints[0,0,k]).z  * h[i,j,k] ;
			}
            }
			}
			
			_StdError[0] = 0D;
			_StdError[1] = 0D;
			_StdError[2] = 0D;	
			
			for (int i = 0; i < _LikelihoodEstimate.GetLength(0); i++) {
            for (int j = 0; j < _LikelihoodEstimate.GetLength(1); j++) {
            for (int k = 0; k < _LikelihoodEstimate.GetLength(2); k++) {
                _StdError[0] = _StdError[0] + Math.Pow(((Point)_QuadraturePoints[i,0,0]).x  - _Theta[0],2) * h[i,j,k] ;
                _StdError[1] = _StdError[1] + Math.Pow(((Point)_QuadraturePoints[0,j,0]).y - _Theta[1],2)  * h[i,j,k] ;
                _StdError[2] = _StdError[2] + Math.Pow(((Point)_QuadraturePoints[0,0,k]).z - _Theta[2],2)  * h[i,j,k] ;
            }
            }
            }

            _StdError[0] =   Math.Sqrt(_StdError[0]);
            _StdError[1] =   Math.Sqrt(_StdError[1]);
            _StdError[2] =   Math.Sqrt(_StdError[2]);

		if(_StdError[0] < 0.3D && _StdError[1] < 0.3D && _StdError[2] < 0.3D ){
			finished = true;
		}



        }
        
        protected double Determinant22(double[,] input){        
			return (input[0,0] * input[1,1]) - (input[1,0] * input[0,1]);
        }
        
        protected double Determinant33(double[,] input){
        
			double d1 = input[0,0] * ((input[2,2] * input[1,1]) - (input[1,2] * input[2,1]));
			double d2 = -1D * input[1,0] * ((input[0,1] * input[2,2]) - (input[2,1] * input[0,2]));
            double d3 = input[2,0] * ((input[0,1] * input[1,2]) - (input[1,1] * input[0,2]));
			return d1 + d2 + d3;
        }  

        protected double[,] Inverse33(double[,] input){
        
			double[,] rtn = new double[3,3];      
			double denominator = (Determinant33(input));

			rtn[0,0] = ((input[1,1] * input[2,2]) - (input[2,1] * input[1,2]))/denominator;
			rtn[0,1] = -1D*((input[1,0] * input[2,2]) - (input[2,0] * input[1,2]))/denominator;
			rtn[0,2] = ((input[1,0] * input[2,1]) - (input[2,0] * input[1,1]))/denominator;

			rtn[1,0] = -1D*((input[0,1] * input[2,2]) - (input[2,1] * input[0,2]))/denominator;
			rtn[1,1] = ((input[0,0] * input[2,2]) - (input[2,0] * input[0,2]))/denominator;
			rtn[1,2] = -1D*((input[0,0] * input[2,1]) - (input[0,1] * input[2,0]))/denominator;

			rtn[2,0] = ((input[0,1] * input[1,2]) - (input[1,1] * input[0,2]))/denominator;
			rtn[2,1] = -1D*((input[0,0] * input[1,2]) - (input[1,0] * input[0,2]))/denominator; 
			rtn[2,2] = ((input[0,0] * input[1,1]) - (input[1,0] * input[0,1]))/denominator;
			
			return rtn;
        }



		protected class KVPDoubleIntComparer2 : IComparer<KeyValuePair<double, int>>
		{
			public int Compare(KeyValuePair<double, int> kvp1, KeyValuePair<double, int> kvp2)
			{
				//Check for equalities: use the same method as the R programming language
				if ((kvp1.Key == kvp2.Key) && (kvp1.Value != kvp2.Value)) {
					if (kvp1.Value > kvp2.Value) {
						return 1;
					}
					else {
						return -1;
					}
				}
				else {
					return (kvp1.Key.CompareTo(kvp2.Key));
				}
			}
		}

		[Serializable]
		protected struct Point{
			public double x;
			public double y;
			public double z;
			public Point(double _x, double _y, double _z){
				x = _x;
				y = _y;
				z = _z;
			}
		}
		
        protected static double[] MatrixMultiply(double[] a, int[] b)
        {
            if (a.Length != b.Length) {
                return null;
            }
            double[] Product = new double[a.Length];

            for (int row = 0; row < a.Length; row++) {
                Product[row] = a[row] * b[row];
            }
            return Product;
        }
        
        protected static double[] MatrixMultiply(double[] a, double[] b)
        {
            if (a.Length != b.Length) {
                return null;
            }
            double[] Product = new double[a.Length];

            for (int row = 0; row < a.Length; row++) {
                Product[row] = a[row] * b[row];
            }
            return Product;
        }
        
        protected static double[,] MatrixMultiply(double[,] a, double[] b)
        {
            if (b.Length != a.GetLength(0)) {
                return null;
            }
            double[,] Product = new double[a.GetLength(0), a.GetLength(1)];

            for (int row = 0; row < a.GetLength(0); row++) {
                for (int col = 0; col < a.GetLength(1); col++) {
                    Product[row, col] = a[row, col] * b[row];
                }
            }
            return Product;
        }
        
        protected static double[,] MatrixMultiply(double[,] a, double[,] b)
        {
            if (b.GetLength(0) != a.GetLength(0)) {
                return null;
            }
            if (b.GetLength(1) != a.GetLength(1)) {
                return null;
            }
            double[,] Product = new double[a.GetLength(0), a.GetLength(1)];

            for (int row = 0; row < a.GetLength(0); row++) {
            for (int col = 0; col < a.GetLength(1); col++) {
            	Product[row, col]=0D;
				for(int k= 0 ; k < a.GetLength(1); k++){
					Product[row, col] = Product[row, col] + (a[row, k] * b[k,col]);
                }
            }
            }
            return Product;
        }        
        
        protected double ArraySum(double[] a)
        {
            double info = 0D;
            for (int i = 0; i < a.Length; i++) {
                info = info + a[i];
            }
            return info;
        }
        protected double ArraySum(double[,] a)
        {
            double info = 0D;
            for (int i = 0; i < a.GetLength(0); i++) {
            for (int j = 0; j < a.GetLength(1); j++) {
            
            	//if(!double.IsNaN(a[i,j])){
					info = info + a[i,j];
                //}
            }
            }
            return info;
        }
        protected double ArraySum(double[,,] a)
        {
            double info = 0D;
            for (int i = 0; i < a.GetLength(0); i++) {
            for (int j = 0; j < a.GetLength(1); j++) {
            for (int k = 0; k < a.GetLength(2); k++) {
				info = info + a[i,j,k];
            }
            }
            }
            return info;
        }
        protected double ArraySum(int[] a)
        {
            int info = 0;
            for (int i = 0; i < a.Length; i++) {
                info = info + a[i];
            }
            return (double)info;
        }        
        protected double[] MatrixReduceBySum(double[,] a)
        {
            double[] sum;
            int row;
            int col;

			sum = new double[a.GetLength(1)];
			for (col = 0; col < a.GetLength(1); col++) {
				for (row = 0; row < a.GetLength(0); row++) {
					sum[col] = sum[col] + a[row, col];
				}
			}
 
            return sum;
        }
	
	public int SelectResponse(string FormItemOID, double[] theta)
	{
        
		
		int ItemIndex = (int)_ItemIDs[FormItemOID];

		Random rnd = new Random(Guid.NewGuid().GetHashCode());
		double selectedCategory =  Math.Round(rnd.NextDouble(),2);
		
		int ResponseOptions = _NumCategoriesByItem[ItemIndex];
		
		int rtn = (ResponseOptions - 1); //4;
		
		double[] CategorySum = new double[ ResponseOptions ];//5
		double[] _Probability = new double[ ResponseOptions ];
		//double sum = 0.0;
		
		double[] CumulativeCategory = new double [ ResponseOptions + 1];
		CumulativeCategory[0] = 1;
		CumulativeCategory[ResponseOptions] = 0; //already zero by initialization
		
		//Console.WriteLine("item: " + FormItemOID + " ItemIndex: " + ItemIndex.ToString() + " Anxiety: " + theta[0].ToString() );
		
		//Console.WriteLine("random number is: " + selectedCategory);
		//Console.WriteLine("_DiscriminationValues[ItemIndex,0]: " + _DiscriminationValues[ItemIndex,0].ToString());
		//Console.WriteLine("_DiscriminationValues[ItemIndex,1]: " + _DiscriminationValues[ItemIndex,1].ToString());
		//Console.WriteLine("_DiscriminationValues[ItemIndex,2]: " + _DiscriminationValues[ItemIndex,2].ToString());		


		
		for (int cat = 0; cat < ResponseOptions - 1; cat++) //0,1,2,3
		{
		double [] locationvector = new double[3];
		locationvector[0] = theta[0] - _CategoryBoundaryValues[ItemIndex][cat];
		//Console.WriteLine("item: " + FormItemOID + " ItemIndex: " + ItemIndex.ToString() + " Threshold[" + cat.ToString() + "]: " + _CategoryBoundaryValues[ItemIndex][cat].ToString() );
		locationvector[1] = theta[1] - _CategoryBoundaryValues[ItemIndex][cat];  
		locationvector[2] = theta[2] - _CategoryBoundaryValues[ItemIndex][cat];
		double sumvector = (_DiscriminationValues[ItemIndex,0] * locationvector[0] + _DiscriminationValues[ItemIndex,1] * locationvector[1] + _DiscriminationValues[ItemIndex,2] * locationvector[2]); 
		CumulativeCategory[cat + 1] = 1 / (1 + Math.Exp((-1) * _LogisticScaling * sumvector));
	
		/*
		Console.WriteLine("sumvector: " + sumvector.ToString());
		Console.WriteLine("locationvector[0]: " + locationvector[0].ToString());
		Console.WriteLine("locationvector[1]: " + locationvector[1].ToString());
		Console.WriteLine("locationvector[2]: " + locationvector[2].ToString());
		Console.WriteLine("cumlativeCategory: " + CumulativeCategory[cat + 1] .ToString());
		*/
		}
		
		
		_Probability[0] = 1 - CumulativeCategory[0];
		_Probability[ ResponseOptions - 1] = CumulativeCategory[ResponseOptions - 1];  

		for (int cat = 0; cat < ResponseOptions; cat++)
		{
			_Probability[cat] = CumulativeCategory[cat] - CumulativeCategory[cat + 1];
			//Console.WriteLine("item: " + FormItemOID + " ItemIndex: " + ItemIndex.ToString() + " Probability[" + cat.ToString() + "]: " + Math.Round((_Probability[cat]*100.0),2).ToString() + " : " +  Math.Round(CumulativeCategory[cat],2).ToString() );
		}
		
		/*
		for (int cat = 0; cat < CategorySum.Length ; cat++)
		{
			if( _Probability[cat] > sum){
				sum = _Probability[cat] ;
				rtn = cat;
			}
		}
		*/
		for (int cat = 0; cat < CategorySum.Length ; cat++)
		{
			if( selectedCategory > Math.Round(CumulativeCategory[cat],2) ){
				rtn = cat -1;
				break;
			}
		}
		
				
		//Console.WriteLine("Answering:" + rtn.ToString() );	
		return rtn;
	}

	}
}

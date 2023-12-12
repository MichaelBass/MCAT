using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;


namespace MSS.Engines
{

    public class MCATEngineGRM : BaseEngineGRM
    {
        public MCATEngineGRM(String itemSelectionMethod, XmlDocument doc, String domainreduction):base(itemSelectionMethod, doc){
			CalculateInformation();
			domainReduction = domainreduction; 
        }
		
		private string domainReduction;
        
        private double[,,,] _ItemInfoOverQuadrature;
        private double[,,,][,] _ItemInfoFunction; 
   
        private double[,,][,] _TestInfoFunction;
        public double[,,][,] TestInfoFunction
        {
            get { return _TestInfoFunction; }
            set
            {
				double [,] a_term2 =Inverse33(_CoVariance);
		
				// Initialize Test Information Function
				for (int qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
				for (int qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
				for (int qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){               
						_TestInfoFunction[qpnt,qpnt2,qpnt3] = a_term2;
				}
				}
				}
            }
        }       
        
        public override void initializeTest(){
			_Theta = new double[3];
			_Theta[0] = 0D;
			_Theta[1] = 0D;
			_Theta[2] = 0D;
			_StdError = new double[3]; 
			_StdError[0] = 9.900;
			_StdError[1] = 9.900;
			_StdError[2] = 9.900;
	
			_DomainCount0 = 0;
			_DomainCount1 = 0;
			_DomainCount2 = 0;
		
        	_PriorDistribution = SetNormalDistribution(_QuadraturePoints, _PriorDistributionMean, _PriorDistributionStdDev);
			InitializeLikelihood(); 
        
			double [,] a_term2 =Inverse33(_CoVariance);
	
			// Initialize Test Information Function
			for (int qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
			for (int qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
			for (int qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){               
				_TestInfoFunction[qpnt,qpnt2,qpnt3] = a_term2;
			}
			}
			}
			
			// Initialize response containers
			_ItemsAsked = new ArrayList();
			_Responses = new ArrayList();
			
			for(int i = 0; i < _ItemsAvailable.Length; i++){
				_ItemsAvailable[i] = 1;
			}
			
        }
       
        public void CalculateInformation()
        {
            int qpnt;
            int qpnt2;
            int qpnt3;
            int item;
            int cat;

            _ItemInfoOverQuadrature = new double[_NumQuadraturePoints,_NumQuadraturePoints,_NumQuadraturePoints, _NumTotalItems];  
            for (item = 0; item < _NumTotalItems; item++)
            {       
				double[,,,]CumulativeCategory = (double[,,,])CumulativeCategoryCollection[item]; 
                for (cat = 0; cat < _NumCategoriesByItem[item]; cat++)
                {
                    for (qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
                    for (qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
                    for (qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){
						Point p = (Point)_QuadraturePoints[qpnt,qpnt2,qpnt3];
                        _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item] = _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item] + Math.Pow( ( CumulativeCategory[qpnt,qpnt2,qpnt3, cat] * (1 - CumulativeCategory[qpnt,qpnt2,qpnt3, cat]) - CumulativeCategory[qpnt,qpnt2,qpnt3, cat + 1] * (1 - CumulativeCategory[qpnt,qpnt2,qpnt3, cat + 1])), 2) / _Probability[qpnt,qpnt2,qpnt3, item, cat];
					}
                    }
                    }
                }

            }		
			_ItemInfoFunction = new double[_NumQuadraturePoints,_NumQuadraturePoints,_NumQuadraturePoints, _NumTotalItems][,];
			_TestInfoFunction = new double[_NumQuadraturePoints,_NumQuadraturePoints,_NumQuadraturePoints][,];

			double [,] a_term2 =Inverse33(_CoVariance);
	
			// Initialize Test Information Function
			for (qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
			for (qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
			for (qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){
					double [,] a_term = new double[3,3];
					a_term[0,0] = 0.0;
					a_term[0,1] = 0.0;
					a_term[0,2] = 0.0;
					a_term[1,0] = 0.0;
					a_term[1,1] = 0.0;
					a_term[1,2] = 0.0;
					a_term[2,0] = 0.0;
					a_term[2,1] = 0.0;
					a_term[2,2] = 0.0;                
					_TestInfoFunction[qpnt,qpnt2,qpnt3] = a_term2;
			}
			}
			}		
			
		    for (item = 0; item < _NumTotalItems; item++)
            {
                for (qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
                for (qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
                for (qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){
					double [,] a_term = new double[3,3];
					
					a_term[0,0] = Math.Pow(_DiscriminationValues[item,0],2) *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					a_term[0,1] = 0.0;// *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					a_term[0,2] = 0.0;
					
					a_term[1,0] = 0.0;// *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					a_term[1,1] = Math.Pow(_DiscriminationValues[item,1],2) *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					a_term[1,2] = 0.0;// *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					
					a_term[2,0] = 0.0;
					a_term[2,1] = 0.0;					
					a_term[2,2] = Math.Pow(_DiscriminationValues[item,2],2) *  _ItemInfoOverQuadrature[qpnt,qpnt2,qpnt3, item];
					_ItemInfoFunction[qpnt,qpnt2,qpnt3, item] = a_term;
					
				}
                }
                }
            }
        }

		private void updateTestInformation(int item){
		
			for (int qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++){
			for (int qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
			for (int qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){
				double [,] a_term = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item];
				double [,] b_term = _TestInfoFunction[qpnt,qpnt2,qpnt3];
				double [,] c_term = new double[3,3];
				
				c_term[0,0] = a_term[0,0] + b_term[0,0];
				c_term[0,1] = a_term[0,1] + b_term[0,1];
				c_term[0,2] = a_term[0,2] + b_term[0,2];
				
				c_term[1,0] = a_term[1,0] + b_term[1,0];
				c_term[1,1] = a_term[1,1] + b_term[1,1];
				c_term[1,2] = a_term[1,2] + b_term[1,2];
				
				c_term[2,0] = a_term[2,0] + b_term[2,0];
				c_term[2,1] = a_term[2,1] + b_term[2,1];
				c_term[2,2] = a_term[2,2] + b_term[2,2];				

				_TestInfoFunction[qpnt,qpnt2,qpnt3] = c_term;
			}
			}
			}

		}

        public override string getCurrentItem(int k)
        {
			int ItemIndex = -1;

			//check if no items available or if done
			int NumItemsAvailable = (int)ArraySum(_ItemsAvailable);
			int i;
			double[] CatInfo = new double[_NumTotalItems];

			//Execute item selection method
			CatInfo = CalcLWInfo(k);
			
			//Eliminate used items
			CatInfo = MatrixMultiply(CatInfo, _ItemsAvailable);
			List<KeyValuePair<double, int>> ItemList = new List<KeyValuePair<double, int>>();
			for (i = 0; i < _NumTotalItems; i++)
			{
				//Don't add zeroes: these are eliminated items
				if (CatInfo[i] != 0 && !_ItemsAsked.Contains(ItemIndex))
				{
					ItemList.Add(new KeyValuePair<double, int>(CatInfo[i], i));
				}
			}
			ItemList.Sort(new KVPDoubleIntComparer2());

if (domainReduction !=String.Empty){

			bool[] _criteria_met = new bool[3];
			_criteria_met[0] = false;
			_criteria_met[1] = false;
			_criteria_met[2] = false;
			
			if(_StdError[0] < 0.3D){
				_criteria_met[0] = true;
			}
			if(_StdError[1] < 0.3D){
				_criteria_met[1] = true;
			}
			if(_StdError[2] < 0.3D){
				_criteria_met[2] = true;
			}
			
			for(k=ItemList.Count-1; k> 0; k--){
				int domain_index = Int32.Parse(_Domains[_Items[ItemList[k].Value].ToString()].ToString());
				
				if(!_criteria_met[domain_index]){
					ItemIndex = ItemList[k].Value;
					break;
				}
			}
}

			if(ItemIndex == -1){
				ItemIndex = ItemList[ItemList.Count - 1].Value;
			}
			
			//ItemIndex = ItemList[0].Value;
			_Variance =  CalcLVariance(ItemIndex);
			updateTestInformation(ItemIndex);
               
            return _Items[ItemIndex].ToString();
        }

        private double[] CalcLWInfo(int i)
        {
        

			//double[,] inverse =  Inverse33(_CoVariance);
			double[] info = new double[_NumTotalItems];

		    for (int item = 0; item < _NumTotalItems; item++)
            {
				double runninginfo = 0D; 
				double a00 =0D;
				double a01 =0D;
				double a02 =0D;
				
				double a10 =0D;
				double a11 =0D;
				double a12 =0D;
				
				double a20 =0D;
				double a21 =0D;
				double a22 =0D;

                for (int qpnt = 0; qpnt < _Probability.GetLength(0); qpnt++) {
                for (int qpnt2 = 0; qpnt2 < _Probability.GetLength(1); qpnt2++){
				for (int qpnt3 = 0; qpnt3 < _Probability.GetLength(2); qpnt3++){

					a00 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][0,0] +  _TestInfoFunction[qpnt,qpnt2,qpnt3][0,0] ;
					a01 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][0,1] +  _TestInfoFunction[qpnt,qpnt2,qpnt3][0,1] ;
					a02 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][0,2] +  _TestInfoFunction[qpnt,qpnt2,qpnt3][0,2] ;
					
					a10 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][1,0] + _TestInfoFunction[qpnt,qpnt2,qpnt3][1,0] ;
					a11 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][1,1] + _TestInfoFunction[qpnt,qpnt2,qpnt3][1,1] ;
					a12 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][1,2] + _TestInfoFunction[qpnt,qpnt2,qpnt3][1,2] ;
					
					a20 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][2,0] + _TestInfoFunction[qpnt,qpnt2,qpnt3][2,0] ;
					a21 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][2,1] + _TestInfoFunction[qpnt,qpnt2,qpnt3][2,1] ;					
					a22 = _ItemInfoFunction[qpnt,qpnt2,qpnt3, item][2,2] + _TestInfoFunction[qpnt,qpnt2,qpnt3][2,2] ;
					
					double[,] input = new double[3,3];
					input[0,0] = a00;
					input[0,1] = a01;
					input[0,2] = a02;
					
					input[1,0] = a10;
					input[1,1] = a11;
					input[1,2] = a12;
					
					input[2,0] = a20;
					input[2,1] = a21;
					input[2,2] = a22;
					double runningDeterminant = Determinant33(input);
					runninginfo = runninginfo + runningDeterminant * _PosteriorProbability[qpnt,qpnt2,qpnt3];
                }
                }
                }
                info[item] = runninginfo;


            }
            
            return info;
        }
    
    
        private double CalcLVariance(int item)
        { 
            double[] tmp1 = new double[3];
			double[] tmp2 = new double[3];
			double[] tmp3 = new double[3];
			double[] tmp4 = new double[3];

            //double[,,] PriorSum = new double[_QuadraturePoints.GetLength(0), _QuadraturePoints.GetLength(1), _QuadraturePoints.GetLength(2)];
            double PriorSum = 0.0D;
			for (int l = 0; l < _QuadraturePoints.GetLength(0); l++) {
			for (int j = 0; j < _QuadraturePoints.GetLength(1); j++) {
			for (int k = 0; k < _QuadraturePoints.GetLength(2); k++) {
				PriorSum = PriorSum +  _LikelihoodEstimate[l,j,k];
			}
			}
			}

			tmp4[0] = 0.0D;
			tmp4[1] = 0.0D;
			tmp4[2] = 0.0D;
			
			for (int cat = 0; cat < _NumCategoriesByItem[item]; cat++)
			{
				tmp1[0] = 0.0D;
				tmp2[0] = 0.0D;
				tmp3[0] = 0.0D;             
				tmp1[1] = 0.0D;
				tmp2[1] = 0.0D;
				tmp3[1] = 0.0D; 
				tmp1[2] = 0.0D;
				tmp2[2] = 0.0D;
				tmp3[2] = 0.0D; 

				//for (int i = 0; i < _QuadraturePoints.Length; i++)// Theta loop{
				for (int l = 0; l < _QuadraturePoints.GetLength(0); l++) {
				for (int j = 0; j < _QuadraturePoints.GetLength(1); j++) {
				for (int k = 0; k < _QuadraturePoints.GetLength(2); k++) {

					tmp1[0] = tmp1[0] + ((Point)_QuadraturePoints[l,j,k]).x * ((Point)_QuadraturePoints[l,j,k]).x * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp2[0] = tmp2[0] + ((Point)_QuadraturePoints[l,j,k]).x * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp3[0] = tmp3[0] + _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];

					tmp1[1] = tmp1[1] + ((Point)_QuadraturePoints[l,j,k]).y * ((Point)_QuadraturePoints[l,j,k]).y * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp2[1] = tmp2[1] + ((Point)_QuadraturePoints[l,j,k]).y * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp3[1] = tmp3[1] + _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];

					tmp1[2] = tmp1[2] + ((Point)_QuadraturePoints[l,j,k]).z * ((Point)_QuadraturePoints[l,j,k]).z * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp2[2] = tmp2[2] + ((Point)_QuadraturePoints[l,j,k]).z * _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];
					tmp3[2] = tmp3[2] + _LikelihoodEstimate[l,j,k]/PriorSum *  _Probability[l,j,k,item,cat];

				}
				}
				}						
				tmp4[0] = tmp4[0] + tmp1[0] - (tmp2[0]*tmp2[0])/tmp3[0]; 
				tmp4[1] = tmp4[1] + tmp1[1] - (tmp2[1]*tmp2[1])/tmp3[1];
				tmp4[2] = tmp4[2] + tmp1[2] - (tmp2[2]*tmp2[2])/tmp3[2];					
			}	
 
            return tmp4[0] + tmp4[1] + tmp4[2];;       
        } 

    }
}

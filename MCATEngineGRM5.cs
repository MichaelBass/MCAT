using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;


namespace MSS.Engines
{

    public class MCATEngineGRM5 : BaseEngineGRM
    {
        public MCATEngineGRM5(String itemSelectionMethod, XmlDocument doc, String domainreduction):base(itemSelectionMethod, doc){
			domainReduction = domainreduction;
			
        }
        
		private string domainReduction;
		
		
        public override void initializeTest(){
			_Theta = new double[3];
			_Theta[0] = 0D;
			_Theta[1] = 0D;
			_Theta[2] = 0D;
			_StdError = new double[3]; 
			_StdError[0] = 9.900;
			_StdError[1] = 9.900;
			_StdError[2] = 9.900;
	
			this._DomainCount0 = 0;
			this._DomainCount1 = 0;
			this._DomainCount2 = 0;
		
        	_PriorDistribution = SetNormalDistribution(_QuadraturePoints, _PriorDistributionMean, _PriorDistributionStdDev);
			InitializeLikelihood(); 
        
			// Initialize response containers
			_ItemsAsked = new ArrayList();
			_Responses = new ArrayList();
			
			for(int i = 0; i < _ItemsAvailable.Length; i++){
				_ItemsAvailable[i] = 1;
			}
			
        }

        public override string getCurrentItem(int k)
        {
			int ItemIndex = -1;

			//check if no items available or if done
			int NumItemsAvailable = (int)ArraySum(_ItemsAvailable);
			int i;
			double[,] CatVarianceInfo = new double[_NumTotalItems,4];
			double[] CatInfo = new double[_NumTotalItems];

			//Execute item selection method
			CatVarianceInfo = CalcLVariance(k);
			
			for (int j = 0; j < _NumTotalItems; j++)
			{			
				CatInfo[j] = CatVarianceInfo[j,0];
			}			
			
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
			
			if(_StdError[0] < 0.3D || this._DomainCount0 > MAX_LENGTH){
				_criteria_met[0] = true;
				if(_StdError[0] > 0.3D && this._DomainCount0 > MAX_LENGTH){
					message = "first domain met item count limit (" + this._DomainCount0.ToString() + "," + this._DomainCount1.ToString() + "," + this._DomainCount2.ToString() + ")";
				}
					
			}
			if(_StdError[1] < 0.3D || this._DomainCount1 > MAX_LENGTH){
				_criteria_met[1] = true;
				if(_StdError[1] > 0.3D && this._DomainCount1 > MAX_LENGTH){
					message = "second domain met item count limit (" + this._DomainCount0.ToString() + "," + this._DomainCount1.ToString() + "," + this._DomainCount2.ToString() + ")";
				}
			}
			if(_StdError[2] < 0.3D || this._DomainCount2 > MAX_LENGTH){
				_criteria_met[2] = true;
				if(_StdError[2] > 0.3D && this._DomainCount2 > MAX_LENGTH){
					message = "third domain met item count limit (" + this._DomainCount0.ToString() + "," + this._DomainCount1.ToString() + "," + this._DomainCount2.ToString() + ")";
				}
			}

			if(_criteria_met[0] && _criteria_met[1] && _criteria_met[2]){
				this.finished = true;
			}

			for(k=0; k< ItemList.Count; k++){
				int domain_index = Int32.Parse(_Domains[_Items[ItemList[k].Value].ToString()].ToString());
				
				if(!_criteria_met[domain_index]){
					ItemIndex = ItemList[k].Value;
					break;
				}
			}
}						
			if(ItemIndex == -1){
				ItemIndex = ItemList[0].Value;
			}
			//ItemIndex = ItemList[0].Value;
			
			_Variance = 0D;
			if (!double.IsNaN(ItemList[0].Key)){
				_Variance = ItemList[0].Key;
			}
			
            return _Items[ItemIndex].ToString();
        }	

		private double[,] CalcLVariance(int i)
        {
            double[,] Variance = new double[_NumTotalItems,4];
            
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

            for (int item = 0; item < _NumTotalItems; item++)
            {
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
				Variance[item,0] = tmp4[0] + tmp4[1] + tmp4[2];
				Variance[item,1] = tmp4[0];
				Variance[item,2] = tmp4[1];
				Variance[item,3] = tmp4[2];
			}  
			
            return Variance;       
        
        }  
    }	
}

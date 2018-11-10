using System;

namespace VGame.Controllers {
    public class ServiceResult {
        public ServiceResult (bool result, object data) {
            this.Result = result;
            this.Data = data;
        }
        public ServiceResult(object data):this(true,data){

        }
        public ServiceResult(Exception ex):this(false,ex.Message){

        }
        public bool Result { get; set; }
        public object Data { get; set; }
        
    }
}
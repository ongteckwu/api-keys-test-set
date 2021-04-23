CONSTANTS.ClOUD_ADMIN_URL = "http://dev.yunshan.ieasy360.com";

var API_CONTEXT = {
    BASIC: "/basic",
    POS: "/pos",
    CRM: "/crm",
    WEIXIN: "/weixin",
    PAYCENTER: "/paycenter",
    BILLSIGN: "/billsign",
    CRMWEB: "/yunshan-crm-web-frontend"
}

var MEITUANSIGNKEY = "5f0t8ajd9k35vmxd";

var INSTALL_ROUTE = {

}

var ROLEUSEABLE = {
    USEABLE: true
}

var YUN_CONNECT={
    CONNECT:false
}

var SERVER_HOST = {
    BASE_HOST: "https://dev.api.yunshan.ieasy360.com",
    ORDER_CENTER: 'http://dev.ordercenter.yunshan.ieasy360.com:80/orders/list',
    ORDER_MISS_CENTER: 'http://dev.ordercenter.yunshan.ieasy360.com:80/wechat/orders/list?appid=pos',
    SCMMOBLIE: 'http://dev.scm-mobile.yunshan.ieasy360.com',
    BILLSIGN: "http://dev.billsign.yunshan.ieasy360.com/billsign/index",
    CRM_WEB: "http://dev.frontend.crm.yunshan.ieasy360.com",
    BASE_REPOT:"http://dev.frontend.pos.yunshan.ieasy360.com",
    RES:"http://dev.api.yunshan.ieasy360.com/reserve",
    RES_WEB:"http://dev.reserve.yunshan.ieasy360.com",
    BAR_CODE:"https://dev.api.yunshan.ieasy360.com/ordercenter/api/v1/mall/ordercenter/updateIsConsumed"
}

var KAFKA_HOST = {
    HOST_19: "192.168.1.19:2181",
    HOST_ALI: "114.55.11.228:2181,114.55.11.228:2182,114.55.54.209:2181",
    HOST_REST: "http://staging.kafka-rest.yunshan.ieasy360.com"
}

var MQTT_HOST = {
    HOST : 'mqf-8vbnwa60r4.mqtt.aliyuncs.com',// 设置当前用户的接入点域名，接入点获取方法请参考接入准备章节文档，先在控制台申请实例
    PORT : 80,//WebSocket协议服务端口，如果是走HTTPS，设置443端口
    ROOTTOPIC : 'topicPOS_dev',//需要操作的Topic
    ACESSKEY : 'LTAIrB5ywOypykdI',//账号的AccessKey，在阿里云控制台查看
    SECRETKEY : 'WCUMOtMZjxHhoDKGxhUoyvwQ6iufwP',//账号的的SecretKey，在阿里云控制台查看
    CLEANSESSION : false,
    GROUPID : 'GID_pos_dev'
    }

var DARK_LAUNCH_URL = {
    UPGRADE: "https://dev.api.yunshan.ieasy360.com/darklaunch/api/ff4j/store/features/posUpgrade",
    CRMDARK: "https://dev.api.yunshan.ieasy360.com/darklaunch/api/ff4j/store/features/crmWebFrontendUpgrade"
};

var VALIDATE_TOKEN_HOST = [
    { HOST_BASIC: "dev.api.yunshan.ieasy360.com", TYPE: TOKEN_HOST_TYPE.POS },
    { HOST_BASIC: "192.168.3.38:8080", TYPE: TOKEN_HOST_TYPE.WEIXIN }
    //{ HOST_BASIC: "192.168.4.41:8084", TYPE: TOKEN_HOST_TYPE.WEIXIN }//周栋环境
];


window.CONTEXT = {
    env: CONSTANTS.EnvConstants.WINDOWS
};

var AUTO_UPDATE = {
    AUTO: true
}

//正念url
var ZN_URL = {
    FRONT_URL: "http://api.test.chainclouds.cn/HtmlApps/html/special/iEasyPos/index.html",//iframe url
    API_TOKEN_URL: "http://115.159.191.202:902/connect/token",                               //获取token
    API_CUSTOMERINFO_URL:"http://182.254.151.86:9046/api/QCTCCustom/Customer/GetCustomerInfo", //��ȡcustomerId��ַ
    API_CANCELORDER_URL:"http://182.254.151.86:9046/api/QCTCCustom/Order/CancerOrder"  //�˵���ַ
}

var WLK_URL = {
    FRONT_URL: "http://api.diangedan.net/portal/wizarpay/memberpay",               //iframe url
    API_TOKEN_URL: "http://api.diangedan.net/member-server/OpenIvkService",           //获取token
    API_CANCELORDER_URL: "http://182.254.151.86:9046/api/QCTCCustom/Order/CancerOrder"  //退单url
}

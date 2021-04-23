package com.Cucumber.framework;

import java.io.File;
import java.io.IOException;
import java.util.concurrent.TimeUnit;

import org.openqa.selenium.Dimension;
import org.openqa.selenium.OutputType;
import org.openqa.selenium.TakesScreenshot;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.chrome.ChromeDriver;
import org.openqa.selenium.chrome.ChromeOptions;
import org.openqa.selenium.firefox.FirefoxDriver;
import org.openqa.selenium.htmlunit.HtmlUnitDriver;
import org.openqa.selenium.ie.InternetExplorerDriver;
import org.openqa.selenium.remote.CapabilityType;
import org.openqa.selenium.remote.DesiredCapabilities;
import org.openqa.selenium.remote.RemoteWebDriver;
import org.openqa.selenium.safari.SafariDriver;
import org.openqa.selenium.support.ui.WebDriverWait;

import com.cucumber.listener.Reporter;

import cucumber.api.Scenario;
import io.appium.java_client.android.AndroidDriver;
import io.appium.java_client.android.AndroidElement;
import io.appium.java_client.service.local.AppiumDriverLocalService;
import io.appium.java_client.service.local.AppiumServiceBuilder;
import io.appium.java_client.service.local.flags.GeneralServerFlag;
import java.net.URL;

/**
 * @ScriptName : StepBase
 * @Description : This class contains generic functionalities like
 *              setup/teardown test environment
 * @Author : Swathin Ratheendren
 * @Creation Date : September 2016 @Modified Date:
 */
public class StepBase {
	// Local Variables
	protected static WebDriver driver;
	protected static Process process;
	protected static Process webkitprocess;
	protected static WebDriverWait webDriverWait;
	protected static Scenario crScenario;
	static DesiredCapabilities capabilities = null; 
	public static String testPlatform;
	public static String testBrowser;

	static AppiumDriverLocalService service;
	static String service_url;

	//BrowserStack credentials
	// static String BS_USERNAME = "swathin1";
	// static String BS_AUTOMATE_KEY = "sgw2vEWegpwtM6txvfSr";

	static String BS_USERNAME = "tahapatel2";
	static String BS_AUTOMATE_KEY = "roqjsEDEWtdYtXXmuzr5";

	static String BrowserStackURL = null;
	/**
	 * @Method: setUp
	 * @Description: This method is used to initialize test execution
	 *               environment i.e. driver initialization, setting
	 *               capabilities for selected device
	 * @author Swathin Ratheendren
	 * @throws Exception 
	 * @Creation Date: September 2016 @Modified Date:
	 */


	public static void setScenario(Scenario cScenario) throws Exception {
		crScenario = cScenario;
	}
	
	@SuppressWarnings("deprecation")
	public static void setUp(String Platform, String Browser) throws Exception {
		try {
			testPlatform = Platform;
			testBrowser = Browser;
			
			if (Platform.equalsIgnoreCase("desktop")||Platform.equalsIgnoreCase("mac")) {
				if(Browser.toLowerCase().equals("chrome"))
				{
					System.setProperty("webdriver.chrome.driver", System.getProperty("user.dir") + "/src/test/java/com/Cucumber/resources/chromedriver.exe");

					//System.setProperty("webdriver.chrome.driver", System.getProperty("user.dir")+ "/src/test/java/com/QuickAssist/Resources/chromedriver.exe");
					capabilities= DesiredCapabilities.chrome();
					capabilities.setCapability(CapabilityType.ACCEPT_SSL_CERTS, true);
					driver = new ChromeDriver(new ChromeOptions());
					//Dimension dime = new Dimension(1440, 900);
					//driver.manage().window().setSize(dime);
					driver.manage().window().maximize();
				}else if(Browser.toLowerCase().equals("firefox")){
					System.setProperty("webdriver.gecko.driver",System.getProperty("user.dir") + "/src/test/java/com/Cucumber/resources/geckodriver.exe");
					//System.setProperty("webdriver.gecko.driver",System.getProperty("user.dir") + "/src/test/java/com/QuickAssist/Resources/geckodriver.exe");
					System.out.println("Executing test on Firefox browser");
					driver = new FirefoxDriver();
					driver.manage().window().maximize();
				}else if(Browser.toLowerCase().equals("ie")){
					System.setProperty("webdriver.ie.driver",System.getProperty("user.dir") + "/src/test/java/com/Cucumber/resources/IEDriverServer.exe");
					System.out.println("Executing test on Internet Explorer browser");
					driver = new InternetExplorerDriver();
					driver.manage().window().maximize();
				}else if(Browser.toLowerCase().equals("edge")){
					System.setProperty("webdriver.edge.driver",System.getProperty("user.dir") + "/src/test/java/com/Cucumber/resources/MicrosoftWebDriver.exe");
					System.out.println("Executing test on Internet Explorer browser");
					driver = new InternetExplorerDriver();
					driver.manage().window().maximize();
				}else if(Browser.toLowerCase().equals("htmlunit")){
					System.out.println("Executing test headlessly on HtmlUnit Driver");
					driver = new HtmlUnitDriver(true);
				}else if(Browser.toLowerCase().equals("safari")){
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(CapabilityType.ACCEPT_SSL_CERTS, true);
					System.out.println("Executing test on Safari browser");
					driver = new SafariDriver(capabilities);
					driver.manage().window().maximize();
				}else if(Browser.toLowerCase().equals("browserstack")){
					capabilities = new DesiredCapabilities();
					//Browser stack
					capabilities.setCapability("browserstack.debug", "true");
					capabilities.setCapability("browserName", "android");
					capabilities.setCapability("platform", "ANDROID");
					capabilities.setCapability("device", "Samsung Galaxy S5");
					System.out.println("Executing test on Browser Stack cloud!");
					//BrowserStack credentials
					String USERNAME = "swathin1";
					String AUTOMATE_KEY = "a7bd305d-db4c-45f3-8cd6-e8f2591223c8";
					String BrowserStackURL = "https://" + USERNAME + ":" + AUTOMATE_KEY + "@hub-cloud.browserstack.com/wd/hub";
					driver = new RemoteWebDriver(new URL(BrowserStackURL), capabilities);
					Thread.sleep(7000);
				}else if(Browser.toLowerCase().equals("saucelabs")){
					String username = "RoyalCyberTest";
					String key = "a7bd305d-db4c-45f3-8cd6-e8f2591223c8";
					String seleniumURI = "@ondemand.saucelabs.com:443";
					String SLurl= "https://" + username + ":" + key + seleniumURI +"/wd/hub";
					capabilities = new DesiredCapabilities();
					capabilities.setCapability("platformName", "Android");
					capabilities.setCapability("deviceName", "Samsung Galaxy S6 Emulator");
					capabilities.setCapability("platformVersion", "5.0");
					capabilities.setCapability("browserName", "");
					capabilities.setCapability("deviceOrientation", "portrait");
					capabilities.setCapability("appiumVersion", "1.5.3");
					System.out.println("Executing test on Saucelabs cloud!");
					driver = new RemoteWebDriver(new URL(SLurl), capabilities);
					Thread.sleep(7000);
				}else if(Browser.toLowerCase().equals("saucelabs-safari")){
					String usernamesls = "RoyalCyberTest";
					String keysls = "a7bd305d-db4c-45f3-8cd6-e8f2591223c8";
					String seleniumURIsls = "@ondemand.saucelabs.com:443";
					String SLurlS= "https://" + usernamesls + ":" + keysls + seleniumURIsls +"/wd/hub";
					// set desired capabilities to launch appropriate browser on Sauce
					capabilities.setCapability(CapabilityType.ACCEPT_SSL_CERTS, true);
					capabilities.setCapability("platform", "OS X 10.11");
					capabilities.setCapability("version", "10.0");
					capabilities.setCapability("browserName", "Safari");
					capabilities.setCapability("name", "Safari_on_MacOSX10.11");
					System.out.println("Executing test on Safari Browser on Saucelabs cloud!");
					driver = new RemoteWebDriver(new URL(SLurlS), capabilities);
					Thread.sleep(7000);
				}else if(Browser.toLowerCase().equals("saucelabs-android")){
					String USERNAME1 = "RoyalCyberTest";
					String ACCESS_KEY = "a7bd305d-db4c-45f3-8cd6-e8f2591223c8";
					String URL = "https://" + USERNAME1 + ":" + ACCESS_KEY + "@ondemand.saucelabs.com:443/wd/hub";
					capabilities = new DesiredCapabilities();
					capabilities.setCapability("testobject_api_key", "856888535B8948439F7BE2C554A5BEC7");
					capabilities.setCapability("platformName", "Android");
					capabilities.setCapability("deviceName", "LG Nexus 5X");
					capabilities.setCapability("platformVersion", "7");
					// capabilities.setCapability("browserName", "Chrome");
					capabilities.setCapability("deviceOrientation", "portrait");
					capabilities.setCapability("appiumVersion", "1.5.3");
					capabilities.setCapability("name","LG-Android_TestRun");
					System.out.println("Executing test on Saucelabs cloud!");
					driver = new AndroidDriver<AndroidElement>(new URL(URL), capabilities);
					Thread.sleep(7000);
				}else if(Browser.toLowerCase().equals("browserstack-android")){
					capabilities = new DesiredCapabilities();
					//capabilities setting for Samsung S8 - Browser stack
					capabilities.setCapability("browserstack.debug", "true");
					capabilities.setCapability("device", "Samsung Galaxy S8");
					capabilities.setCapability("realMobile", "true");
					capabilities.setCapability("os_version", "7.0");
					capabilities.setCapability("unicodeKeyboard", "true");
					System.out.println("Executing test on Samsung S8(Android) on Browser Stack cloud!");
					BrowserStackURL= "https://" + BS_USERNAME + ":" + BS_AUTOMATE_KEY + "@hub-cloud.browserstack.com/wd/hub";
					driver = new RemoteWebDriver(new URL(BrowserStackURL), capabilities);
				}else if(Browser.toLowerCase().equals("browserstack-safari")){
					capabilities = new DesiredCapabilities();
					//capabilities setting for Mac Safari Browser - Browser stack
					capabilities.setCapability(CapabilityType.ACCEPT_SSL_CERTS, true);
					capabilities.setCapability("browserstack.debug", "true");
					capabilities.setCapability("browserName", "Safari");
					capabilities.setCapability("platform", "MAC");
					System.out.println("Executing test on Safari-Mac on Browser Stack cloud!");
					BrowserStackURL= "https://" + BS_USERNAME + ":" + BS_AUTOMATE_KEY + "@hub-cloud.browserstack.com/wd/hub";
					driver = new RemoteWebDriver(new URL(BrowserStackURL), capabilities);
				}else {
					System.out.println("Provide valid browser choice in config file!");
				}
			
			driver.manage().timeouts().implicitlyWait(Integer.parseInt(System.getProperty("test.implicitlyWait")), TimeUnit.SECONDS);
			//driver.manage().timeouts().pageLoadTimeout(Integer.parseInt(System.getProperty("test.pageLoadTimeout")), TimeUnit.SECONDS);	
		}
		/*else if (Platform.equalsIgnoreCase("android")) {

				switch(System.getProperty("test.AppType").toLowerCase())
				{
				case "webapp" : 
					// Set the capabilities for AndroidDriver
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.BROWSER_NAME, Browser);
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					//capabilities.setCapability(MobileCapabilityType.UDID, "192.168.1.100:5555");
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.DEVICE_READY_TIMEOUT, "480000");
					//capabilities.setCapability("unicodeKeyboard", "true");
					//Thread.sleep(10000);
					driver = new AndroidDriver<>(new URL("http://0.0.0.0:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					break;

				case "nativeapp" :
					// Set the capabilities for AndroidDriver
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.DEVICE_READY_TIMEOUT, "480000");
					capabilities.setCapability("appPackage", System.getProperty("test.appPackage"));
					capabilities.setCapability("appActivity", System.getProperty("test.appActivity"));
					System.out.println("Appium_Port_StepBase_D2_: "+HashMapContainer.get("port"));
					//driver = new AndroidDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					driver = new AndroidDriver<>(new URL("http://0.0.0.0:4723"+"/wd/hub"), capabilities);
					break;

				case "hybridapp" :
					// Set the capabilities for AndroidDriver
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.DEVICE_READY_TIMEOUT, "480000");
					capabilities.setCapability("appPackage", System.getProperty("test.appPackage"));
					capabilities.setCapability("appActivity", System.getProperty("test.appActivity"));
					System.out.println("Appium_Port_StepBase_D2_: "+HashMapContainer.get("port"));
					driver = new AndroidDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					break;

				default: 
					System.out.println("Invalid value set for test property - test.AppType!");
					System.out.println("Enter one of the following options: webapp | nativeapp | hybridapp ");
					System.out.println("Setting capabilities for WebApp..");
					// Set the capabilities for AndroidDriver
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.BROWSER_NAME, Browser);
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.DEVICE_READY_TIMEOUT, "480000");
					System.out.println("Appium_Port_StepBase_D2_: "+HashMapContainer.get("port"));
					driver = new AndroidDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					break;
				}	
				if(System.getProperty("test.AppType").equals("webapp")){
					Set<String> contextNames = ((AppiumDriver<?>) driver).getContextHandles();
					for (String contextName : contextNames) {
						if (contextName.contains("WEBVIEW_"))
						{
							((AppiumDriver<?>) driver).context(contextName);
						}
					}
				}

				driver.manage().timeouts().implicitlyWait(Integer.parseInt(System.getProperty("test.implicitlyWait")), TimeUnit.SECONDS);
				if(Platform.equals("desktop")||System.getProperty("test.AppType").equals("webapp")){
					//driver.manage().timeouts().pageLoadTimeout(Integer.parseInt(System.getProperty("test.pageLoadTimeout")), TimeUnit.SECONDS);
				}
			}*/

		/*else if (Platform.equalsIgnoreCase("ios")) {

				switch(System.getProperty("test.AppType").toLowerCase())
				{
				case "webapp" :
					// Set the capabilities for IOSDriver
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.BROWSER_NAME, Browser);
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoAcceptAlerts", true);
					//capabilities.setCapability("autoWebview", true);
					capabilities.setCapability("safariAllowPopups", true);
					//capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability("showIOSLog", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//	capabilities.setCapability(MobileCapabilityType.LAUNCH_TIMEOUT, "480000");
					driver = new IOSDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					//driver = new IOSDriver<>(new URL("http://0.0.0.0:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					Thread.sleep(10000);
					break;

				case "nativeapp" :
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					//capabilities.setCapability("autoAcceptAlerts", true);
					//capabilities.setCapability("autoWebview", true);
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability("showIOSLog", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.LAUNCH_TIMEOUT, "480000");
					capabilities.setCapability("bundleId", System.getProperty("test.appBundleID"));
					driver = new IOSDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					Thread.sleep(10000);
					break;

				case "hybridapp" :
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoAcceptAlerts", true);
					//capabilities.setCapability("autoWebview", true);
					capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability("showIOSLog", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.LAUNCH_TIMEOUT, "480000");
					capabilities.setCapability("bundleId", System.getProperty("test.appBundleID"));
					driver = new IOSDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					Thread.sleep(10000);
					break;

				default: 

					System.out.println("Invalid value set for test property - test.AppType!");
					System.out.println("Enter one of the following options: webapp | nativeapp | hybridapp ");
					System.out.println("Setting capabilities for WebApp..");
					capabilities = new DesiredCapabilities();
					capabilities.setCapability(MobileCapabilityType.BROWSER_NAME, Browser);
					capabilities.setCapability(MobileCapabilityType.PLATFORM_NAME, Platform);
					capabilities.setCapability(MobileCapabilityType.VERSION, HashMapContainer.get("version"));
					capabilities.setCapability(MobileCapabilityType.DEVICE_NAME,HashMapContainer.get("deviceName"));
					capabilities.setCapability(MobileCapabilityType.UDID, HashMapContainer.get("udid"));
					capabilities.setCapability("autoAcceptAlerts", true);
					//capabilities.setCapability("autoWebview", true);
					capabilities.setCapability("safariAllowPopups", true);
					//capabilities.setCapability("autoDismissAlerts", true);
					capabilities.setCapability("showIOSLog", true);
					capabilities.setCapability(MobileCapabilityType.AUTOMATION_NAME, "Appium");
					capabilities.setCapability(MobileCapabilityType.NEW_COMMAND_TIMEOUT, "480");
					//capabilities.setCapability(MobileCapabilityType.LAUNCH_TIMEOUT, "480000");
					driver = new IOSDriver<>(new URL("http://127.0.0.1:"+HashMapContainer.get("port")+"/wd/hub"), capabilities);
					Thread.sleep(10000);
				}

				Set<String> contextNames = ((AppiumDriver<?>) driver).getContextHandles();
				for (String contextName : contextNames) {
					if (contextName.contains("WEBVIEW_"))
					{
						((AppiumDriver<?>) driver).context(contextName);
					}
				}
			}*/
	} catch (Exception e) {
		e.printStackTrace();
		throw e;
	}
}

public static void appiumStart(String portNo, String appiumNodePath, String appiumJSPath, String appName) throws Exception {

	//Path appPath = null;

	if(System.getProperty("test.AppType").toLowerCase().equals("webapp")){
		service = AppiumDriverLocalService.buildService(new AppiumServiceBuilder().usingPort(Integer.parseInt(portNo))			
				.usingDriverExecutable(new File(appiumNodePath)).withAppiumJS(new File(appiumJSPath)).withArgument(GeneralServerFlag.SESSION_OVERRIDE));
	}else{
		/*			//Based on device Type
			if(testPlatform.equalsIgnoreCase("android")){
				appPath = Paths.get(System.getProperty("user.dir")+ "/src/test/java/com/Cucumber/pageObjects/android/" + appName +".apk");
			}else if(testPlatform.equalsIgnoreCase("ios")){
				appPath = Paths.get(System.getProperty("user.dir")+ "/src/test/java/com/Cucumber/pageObjects/iOS/" + appName + ".ipa");
			}
			Path absoluteAppPath = appPath.toAbsolutePath();
			service = AppiumDriverLocalService.buildService(new AppiumServiceBuilder().usingPort(Integer.parseInt(portNo))			
					.usingDriverExecutable(new File(appiumNodePath)).withAppiumJS(new File(appiumJSPath)).withArgument(GeneralServerFlag.SESSION_OVERRIDE));*/
	}
	//service.start();
	//Thread.sleep(25000);
	//service_url = service.getUrl().toString();
}

public static void appiumStop() throws Exception {
	service.stop();
}

/**
 * @Method: getDriver
 * @Description: This method returns appium driver instance.
 * @return :Appium Driver instance
 * @author Swathin Ratheendren
 * @Creation Date: September 2016 @Modified Date:
 */
public static WebDriver getDriver() {
	return driver;
}

/**
 * @Method: tearDown
 * @Description: this method is used to close the appium driver instance.
 * @author Swathin Ratheendren
 * @throws IOException
 * @Creation Date: September 2016 @Modified Date:
 */
public static void tearScenario(Scenario scenario) {
	try {
		if (scenario.isFailed()) {
			StepBase.embedScreenshot();
		}
	} catch (Exception e) {
		e.printStackTrace();
	}
}	

public static void tearDown() {
	try{
		if(driver!=null)
		{
			if(testPlatform.equals("desktop")||System.getProperty("test.AppType").equals("webapp"))
			{
				driver.manage().deleteAllCookies();
			}
			driver.quit();
			driver=null;
			Thread.sleep(10000);
		}
	} catch (Exception e) {
		e.printStackTrace();
	}
}

/**
 * Method: embedScreenshot Description: This method attach screenshot to the
 * cucumber report.
 * 
 * @author Swathin Ratheendren
 * @Creation Date: September 2016 Modified Date:
 */
public static void embedScreenshot() {
	try{
		if(System.getProperty("test.DisableScreenShotCapture").equalsIgnoreCase("false"))
		{
			Thread.sleep(1000);
			final byte[] screenshot = ((TakesScreenshot) driver).getScreenshotAs(OutputType.BYTES);
			crScenario.embed(screenshot, "image/png"); // Stick it to HTML report
		}
	}
	catch(Exception e){
		e.printStackTrace();
	}
}

public static void embedProvidedScreenshot(byte[] screenshot) {
	try{
		if(System.getProperty("test.DisableScreenShotCapture").equalsIgnoreCase("false"))
		{
			Thread.sleep(1000);
			crScenario.embed(screenshot, "image/png"); // Stick it to HTML report
		}else{
			throw new Exception("Test Property - test.ScreenShotCapture is disabled!");
		}
	}
	catch(Exception e){
		e.printStackTrace();
	}
}
}

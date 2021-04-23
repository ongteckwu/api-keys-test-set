---
 title: java在Java集成测试中查询CosmosDB时代码卡住堆栈内存溢出
 date: 2020-08-31
 tags: 问题锦集
---

## 原文标题:
{% raw %}
Code stuck when querying CosmosDB in java integration test
{% endraw %}


## 问题:
{% raw %}
我正在为API编写集成测试。 在DAO类中，当我尝试使用AsyncDocumentClient实例queryDocuments方法从Cosmos DB中获取数据时，程序会无限期地卡住。
{% endraw %}
{% raw %}
这是我的函数调用的样子：
{% endraw %}

```
 Iterator< FeedResponse<Document>> getDataFromCosmos = asyncDocumentClientInstance.queryDocuments(collectionLink, querySpec, feedOptions).toBlocking().getIterator();
```
{% raw %}
仅当我运行测试用例（在测试模式下）时，才会发生这种情况。 但是在正常模式下运行时，代码和API可以正常工作。
{% endraw %}
{% raw %}
这是我的测试类的样子：（我正在进行集成测试，而不是单元测试）
{% endraw %}

```
 import org.junit.runner.RunWith; import org.springframework.test.context.junit4.SpringJUnit4ClassRunner; @RunWith(SpringJUnit4ClassRunner.class) public class ApiTest extends TestngInitializer { @Autowired ServiceImpl serviceImpl; @Test public void apiTest() { RequestClass requestObject = new requestObject(); // hardcoding requestObject data variables using setters ResponseClass response = serviceImpl.apiMethod(requestObject,HttpHeaders); //Assertions on response } }
```
{% raw %}
我的TestngInitializer类看起来像（设置获取所有spring bean的路径）
{% endraw %}

```
 @ContextConfiguration(locations = {"classpath:/META-INF/biz-context.xml","classpath:/META-INF/service-context.xml"}) public class TestngInitializer { }
```
{% raw %}
当我尝试逐行调试时，当控制权到达BlockingObservable.java ，控制权会无限期地等待某个线程完成，而在正常模式下运行代码则不是这种情况。
{% endraw %}

```
 BlockingUtils.awaitForComplete(latch, subscription);
```
{% raw %}
如果有人可以帮助我，那就太好了。
{% endraw %}
{% raw %}
这也是我尝试过的可复制的小代码：
{% endraw %}

```
 @Test public void testDBLocally() { AsyncDocumentClient asyncDocumentClient = readDocumentClient(); //calling below function FeedOptions feedOptions = new FeedOptions(); feedOptions.setEnableCrossPartitionQuery(true); String collectionLink = "/dbs/link_to_collection"; SqlQuerySpec querySpec = new SqlQuerySpec("select * from some_document c "); //here I tried two approaches //first approach using iterator Iterator<FeedResponse<Document>> iterator = asyncDocumentClient.queryDocuments(collectionLink,querySpec,feedOptions).toBlocking().getIterator(); while(iterator.hasNext()) { //control stuck indefinitely at iterator.hasNext() //control never reached inside while loop } //second approach using last() FeedResponse<Document> response = capsReadDocumentClient.queryDocuments(collectionLink, querySpec, feedOptions).toBlocking().last(); //control stuck here documentList = response.getResults(); //control never reached here } public AsyncDocumentClient readDocumentClient() { String HOST = "https://azure-cosmosdb.documents.azure.com:443/"; String MASTER_KEY = "qQOkeLmJNa-w.6BA-5xaz4xxmy8O3MMj2D92V~3PukmUNeGyOKm~_40Oi"; return new AsyncDocumentClient.Builder() .withServiceEndpoint(HOST) .withMasterKeyOrResourceToken(MASTER_KEY) .withConnectionPolicy(ConnectionPolicy.GetDefault()) .withConsistencyLevel(ConsistencyLevel.Session) .build(); }
```

## 解决方案:

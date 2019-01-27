# ☄️ The cscore Library

`cscore` is a minimal, zero-dependency collection of common patterns & helpers needed in most C# projects. It can be used in both pure **C#** and **Unity** projects. 

[**Website**](https://www.csutil.com/projects/cscore) 
**•**
[**GitHub**](https://github.com/cs-util-com/cscore) 
**•**
[**Examples**](#💡-Usage-&-Examples) 
**•**
[**Getting started**](#💾-Installation)

#  Overview 
See the [examples](#💡-Usage-&-Examples) below to get a quick overview of all library features:


### Pure C# Components
* [Log](#Logging) - A minimalistic logging wrapper 
* [EventBus](#The-EventBus) - Publish and subscribe to global events from anywhere in your code. Sends **1 million events in under 3 seconds** with minimal memory footprint!
* [Injection Logic](#Injection-Logic) - A simple inversion of control pattern that does not rely on magic. Relies on the EventBus system, so its super fast as well!
* [JSON Parsing](#JSON-Parsing) - Reading and writing JSON through a simple interface. Default implementation uses [Json.NET](https://github.com/JamesNK/Newtonsoft.Json) to ensure high performance
* [REST Extensions](#REST-Extensions) - Extensions to simplify sending REST requests in as few lines as possible without limiting flexibility
* [Directory & File Extensions](#Directory-&-File-Extensions) - To simplify handling files, folders and persisting data
* Common String extension methods demonstrated in StringExtensionTests.cs
* Many other helpfull extension methods best demonstrated in HelperMethodTests.cs


### Additional Unity Components
* [GameObject.Subscribe() & MonoBehaviour.Subscribe()](#`GameObject.Subscribe()`-&-`MonoBehaviour.Subscribe()`) - Listening to events while respecting the livecycle of Unity objects
* [MonoBehaviour Injection & Singletons](#MonoBehaviour-Injection-&-Singletons) - Using the injection logic to create and access Unity objects 
* [The Link Pattern](#The-`Link`-Pattern) - Making it easy to connect prefabs with code (and by that separate design & UI from your logic)
* [MonoBehaviour.ExecuteDelayed & MonoBehaviour.ExecuteRepeated](#`MonoBehaviour.ExecuteDelayed`-&-`MonoBehaviour.ExecuteRepeated`) - Executing asyncronous actions delayed and/or repeated
* [UnityWebRequest.SendV2()](#`UnityWebRequest.SendV2()`) - UnityWebRequest extension methods
* PlayerPrefsV2 that adds Bool as a type and encrypted strings, see PlayerPrefsV2Tests.cs for examples


### Status
![](https://img.shields.io/badge/Maintained%3F-yes-green.svg?style=flat-square)
![](https://img.shields.io/github/last-commit/cs-util-com/cscore.svg?colorB=4267b2&style=flat-square)
![](https://img.shields.io/github/issues-closed/cs-util-com/cscore.svg?colorB=006400&style=flat-square)
[![](https://badge.waffle.io/cs-util-com/cscore.svg?columns=all&style=flat-square)](https://waffle.io/cs-util-com/cscore)

* To get started, see the [installation instructions](#💾-Installation) below.
* To ensure full test coverage mutation testing is used (thanks to [Stryker](https://github.com/stryker-mutator/stryker-net)!)
* To get in contact and stay updated [see the links below](#How-to-get-in-contact)




# 💡 Usage & Examples
See below for a full usage overview to explain the APIs with simple examples.


## Logging

```cs
Log.d("I'm a log message");
Log.w("I'm a warning");
Log.e("I'm an error");
Log.e(new Exception("I'm an exception"));
Log.w("I'm a warning with parmas", "param 1", 2, "..");
```

This will result in the following output in the Log:
```
> I'm a log message
  at LogTests.TestBasicLogOutputExamples() 

> WARNING: I'm a warning
  at LogTests.TestBasicLogOutputExamples() 

>>> ERROR: I'm an error
    at Log.e(System.String error, System.Object[] args) c:\...\Log.cs:line 25
     LogTests.TestBasicLogOutputExamples() c:\...\LogTests.cs:line 15

>>> EXCEPTION: System.Exception: I'm an exception
    at Log.e(System.Exception e, System.Object[] args) c:\...\Log.cs:line 29
     LogTests.TestBasicLogOutputExamples() c:\...\LogTests.cs:line 16

> WARNING: I'm a warning with parmas : [[param 1, 2, ..]]
  at LogTests.TestBasicLogOutputExamples()
```



## AssertV2
`AssertV2` can be used anywhere in your code, it will be automatically removed from your production code:
```cs
AssertV2.IsTrue(1 + 1 == 3, "This assertion will fail");
```


## Log.MethodEntered

Simple monitoring of method calls and method-timings to detect abnormal behavior:
```cs
private void SomeExampleMethod1(string s, int i) {
    Stopwatch timing = Log.MethodEntered("s=" + s, "i=" + i);
    
    { // .. here would be some method logic ..
        Thread.Sleep(3);
    } // .. as the last line in the tracked method add:
    
    Log.MethodDone(timing, maxAllowedTimeInMs: 50);
    // If the method needed more then 50ms an error is logged
}
```

This will result in the following output in the Log:
```cs
>  --> LogTests.SomeExampleMethod1(..) : [[s=I am a string, i=123]]
  at LogTests.SomeExampleMethod1(System.String s, Int32 i) 

>     <-- LogTests.SomeExampleMethod1(..) finished after 3 ms
  at LogTests.SomeExampleMethod1(System.String s, Int32 i) 
```

## The EventBus

```cs
// The EventBus can be accessed via EventBus.instance
EventBus eventBus = EventBus.instance;
string eventName = "TestEvent1";

//Register a subscriber for the eventName that gets notified when ever an event is send:
object subscriber1 = new object(); // can be of any type
eventBus.Subscribe(subscriber1, eventName, () => {
    Log.d("The event was received!");
});

// Now send out an event:
eventBus.Publish(eventName);

// When subscribers dont want to receive events anymore they can unsubscribe:
eventBus.Unsubscribe(subscriber1, eventName);
```


__Rule of thumb__: Only use an `EventBus` if you can't exactly tell who will listen to the published events. Do not use the `EventBus` to pass an event from x to y if you know exactly who x and y will be! Atificially separating 2 components that tightly belong together does not help


## Injection Logic
```cs
// The default injector can be accessed via IoC.inject
Injector injector = IoC.inject;

// Requesting an instance of MyClass1 will fail because no injector registered yet to handle requests for the MyClass1 type:
Assert.Null(injector.Get<MyClass1>(this));

// Setup an injector that will always return the same instance for MyClass1 when IoC.inject.Get<MyClass1>() is called:
MySubClass1 myClass1Singleton = new MySubClass1();
injector.SetSingleton<MyClass1, MySubClass1>(myClass1Singleton);

// Internally .SetSingleton() will register an injector for the class like this:
injector.RegisterInjector<MyClass1>(new object(), (caller, createIfNull) => {
    // Whenever injector.Get is called the injector always returns the same instance:
    return myClass1Singleton;
});

// Now calling IoC.inject.Get<MyClass1>() will always result in the same instance:
MyClass1 myClass1 = injector.Get<MyClass1>(this);
Assert.Same(myClass1Singleton, myClass1); // Its the same object reference
```

Another extended example usage can be found in `InjectionTests.ExampleUsage2()`


## JSON Parsing 
The JsonWriter and JsonReader interfaces are an abstraction that should be flexiable enough to be used for most usecases. The underlying implementation can easily be swapped of needed and the default implementation uses [Json.NET](https://github.com/JamesNK/Newtonsoft.Json).

```cs
class MyClass1 { // example class with a field and a property
    public string myString;
    public string myString2 { get; set; }
}

MyClass1 x1 = new MyClass1() { myString = "abc", myString2 = "def" };

// Generate a json object from the object that includes all public fields and props:
string jsonString = JsonWriter.GetWriter().Write(x1);

// Parse the json string back into a second instance x2 and compare both:
MyClass1 x2 = JsonReader.GetReader().Read<MyClass1>(jsonString);
Assert.Equal(x1.myString, x2.myString);
Assert.Equal(x1.myString2, x2.myString2);
```


## REST Extensions 
```cs
// The property names are based on the https://httpbin.org/get json response:
class HttpBinGetResp { 
    public string origin { get; set; }
    public Dictionary<string, object> headers { get; set; }
}

RestRequest request = new Uri("https://httpbin.org/get").SendGET();

// Send the request and parse the response into the HttpBinGetResp class:
HttpBinGetResp response = await request.GetResult<HttpBinGetResp>();
Log.d("Your external IP is " + response.origin);
```


## Directory & File Extensions 
The [DirectoryInfo](https://docs.microsoft.com/en-us/dotnet/api/system.io.directoryinfo) and [FileInfo](https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo) classes already provide helpful interfaces to files and directories and the following extensions improve the usability if these classes:

```cs
// Get a directory to work in:
DirectoryInfo myDirectory = EnvironmentV2.instance.GetAppDataFolder();
Log.d("The directory path is: " + myDirectory.FullPath());

// Get a non-existing child directory
var childDir = myDirectory.GetChildDir("MyExampleSubDirectory1");

// Create the sub directory:
childDir.CreateV2(); // myDirectory.CreateSubdirectory("..") works too

// Rename the directory:
childDir.Rename("MyExampleSubDirectory2");

// Get a file in the child directory:
FileInfo file1 = childDir.GetChild("MyFile1.txt");

// Saving and loading from files:
string someTextToStoreInTheFile = "Some text to store in the file";
file1.SaveAsText(someTextToStoreInTheFile);
string loadedText = file1.LoadAs<string>(); // loading JSON would work as well
Assert.Equal(someTextToStoreInTheFile, loadedText);

// Deleting directories:
Assert.True(childDir.DeleteV2()); // (Deleting non-existing directories would returns false)
// Check that the directory no longer exists:
Assert.False(childDir.IsNotNullAndExists());
```



# Unity Component Examples
There are additional components specifically created for Unity, that will be explained below:

## `GameObject.Subscribe()` & `MonoBehaviour.Subscribe()`

There are extension methods for both `GameObjects` and `Behaviours` which internally handle the lifecycle of their subscribers correctly. If a `GameObject` for example is currently not active or was destroyed the published events will not reach it.

```cs
// GameObjects can subscribe to events:
var myGameObject = new GameObject("MyGameObject 1");
myGameObject.Subscribe("MyEvent1", () => {
    Log.d("I received the event because I'm active");
});

// Behaviours can subscribe to events too:
var myExampleMono = myGameObject.GetOrAddComponent<MyExampleMono1>();
myExampleMono.Subscribe("MyEvent1", () => {
    Log.d("I received the event because I'm enabled and active");
});

// The broadcast will reach both the GameObject and the MonoBehaviour:
EventBus.instance.Publish("MyEvent1");
```


## MonoBehaviour Injection & Singletons
Often specific `MonoBehaviours` should only exist once in the complete scene, for this scenario `IoC.inject.GetOrAddComponentSingleton()` and `IoC.inject.GetComponentSingleton()` can be used.

```cs
// Initially there is no MonoBehaviour registered in the system:
Assert.IsNull(IoC.inject.Get<MyExampleMono1>(this));

// Calling GetOrAddComponentSingleton will create a singleton:
MyExampleMono1 x1 = IoC.inject.GetOrAddComponentSingleton<MyExampleMono1>(this);

// Calling GetOrAddComponentSingleton again now returns the singleton:
MyExampleMono1 x2 = IoC.inject.GetOrAddComponentSingleton<MyExampleMono1>(this);
Assert.AreSame(x1, x2); // Both references point to the same object

// Calling the default IoC.inject.Get will also return the same singleton:
MyExampleMono1 x3 = IoC.inject.Get<MyExampleMono1>(this);
Assert.AreSame(x1, x3); // Both references point to the same object
```

Calling `GetOrAddComponentSingleton` will create a singleton. The parent gameobject of this singleton will be created together with it in the scene. The location of the singleton will be:

`"Singletons" GameObject` -> `"MyExampleMono1" GameObject` -> `MyExampleMono1`

This way all created singletons will be created and grouped together in the `"Singletons" GameObject` and accessible like any other MonoBehaviour as well.



## The `Link` Pattern
Connecting prefabs created by designers with internal logic often is beneficial to happen in a central place. To access all required parts of the prefab the `Link` pattern and helper methods like `gameObject.GetLinkMap()` can be used:
```cs
// Load a prefab that contains Link MonoBehaviours:
GameObject prefab = ResourcesV2.LoadPrefab("ExamplePrefab1.prefab");

// Collect all Link MonoBehaviours in the prefab:
Dictionary<string, Link> links = prefab.GetLinkMap();

// Via the Link.id the objects can quickly be accessed: 
Assert.IsNotNull(links.Get<GameObject>("Button 1"));

// The GameObject "Button 1" contains a Button-Mono that can be accessed:
Button button1 = links.Get<Button>("Button 1");
button1.SetOnClickAction(delegate {
    Log.d("Button 1 clicked");
});

// The prefab also contains other Links in other places to quickly setup the UI:
links.Get<Text>("Text 1").text = "Some text";
links.Get<Toggle>("Toggle 1").SetOnValueChangedAction((isNowChecked) => {
    Log.d("Toggle 1 is now " + (isNowChecked ? "checked" : "unchecked"));
    return true;
});
```


## `MonoBehaviour.ExecuteDelayed` & `MonoBehaviour.ExecuteRepeated`
```cs
// Execute a task after a defined time:
myMonoBehaviour.ExecuteDelayed(() => {
    Log.d("I am executed after 0.6 seconds");
}, delayInSecBeforeExecution: 0.6f);

// Execute a task multiple times:
myMonoBehaviour.ExecuteRepeated(() => {
    Log.d("I am executed every 0.3 seconds until I return false");
    return true;
}, delayInSecBetweenIterations: 0.3f, delayInSecBeforeFirstExecution: .2f);
```

Additionally there is myMono.StartCoroutinesInParallel(..) and myMono.StartCoroutinesSequetially(..), see TODO for details



## `UnityWebRequest.SendV2()` 
It is recommended to use the `Uri` extension methods for requests (see TODO). If `UnityWebRequest` has to be used, then `UnityWebRequest.SendV2()` should be a good alternative. `SendV2` creates the same `RestRequest` objects that the `Uri` extension methods create as well. 

```cs
RestRequest request1 = UnityWebRequest.Get("https://httpbin.org/get").SendV2();
Task<HttpBinGetResp> requestTask = request1.GetResult<HttpBinGetResp>();
yield return requestTask.AsCoroutine();
HttpBinGetResp response = requestTask.Result;
Log.d("Your IP is " + response.origin);

// Alternatively the asynchronous callback in GetResult can be used:
UnityWebRequest.Get("https://httpbin.org/get").SendV2().GetResult<HttpBinGetResp>((result) => {
    Log.d("Your IP is " + response.origin);
});
```




# 💾 Installation

## 📦 Installing cscore into pure C# projects

 `cscore` can be installed via [NuGet](https://www.nuget.org/profiles/csutil.com), just add the following lines to the root of your `.csproj` file: 

``` XML
<ItemGroup>
    <PackageReference Include="com.csutil.cscore" Version="*" />
</ItemGroup>
```

After adding the references, install the packages by executing `dotnet restore` inside the project folder.

## 🎮 Installing cscore into Unity projects
Download the Unity package from the release page.




# 💚 Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
<!---
See current features in development here: https://github.com/cs-util-com/cscore/projects/1
-->

## How to get in contact

[![Twitter](https://img.shields.io/twitter/follow/csutil_com.svg?style=for-the-badge&logo=twitter)](https://twitter.com/intent/follow?screen_name=csutil_com)
[![Discord](https://img.shields.io/discord/518684359667089409.svg?logo=discord&label=chat%20on%20discord&style=for-the-badge)](https://discord.gg/bgGqRe)
[![Gitter](https://img.shields.io/gitter/room/csutil-com/community.svg?style=for-the-badge&logo=gitter-white)](https://gitter.im/csutil-com)

To stay updated via Email see https://www.csutil.com/updates

# License

![](https://img.shields.io/github/license/cs-util-com/cscore.svg?style=for-the-badge)

[![csutil.com](https://forthebadge.com/images/badges/built-with-love.svg)](https://www.csutil.com/)

<!--- // Other very important badges:
![](https://forthebadge.com/images/badges/made-with-c-sharp.svg)
![](https://forthebadge.com/images/badges/does-not-contain-treenuts.svg)
![](https://forthebadge.com/images/badges/contains-cat-gifs.svg)
![](https://forthebadge.com/images/badges/as-seen-on-tv.svg)
-->

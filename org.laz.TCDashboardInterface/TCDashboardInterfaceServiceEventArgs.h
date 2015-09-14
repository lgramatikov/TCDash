//-----------------------------------------------------------------------------
// <auto-generated> 
//   This code was generated by a tool. 
// 
//   Changes to this file may cause incorrect behavior and will be lost if  
//   the code is regenerated.
//
//   Tool: AllJoynCodeGenerator.exe
//
//   This tool is located in the Windows 10 SDK and the Windows 10 AllJoyn 
//   Visual Studio Extension in the Visual Studio Gallery.  
//
//   The generated code should be packaged in a Windows 10 C++/CX Runtime  
//   Component which can be consumed in any UWP-supported language using 
//   APIs that are available in Windows.Devices.AllJoyn.
//
//   Using AllJoynCodeGenerator - Invoke the following command with a valid 
//   Introspection XML file and a writable output directory:
//     AllJoynCodeGenerator -i <INPUT XML FILE> -o <OUTPUT DIRECTORY>
// </auto-generated>
//-----------------------------------------------------------------------------
#pragma once

namespace org { namespace laz { namespace TCDashboardInterface {

// Methods
public ref class TCDashboardInterfaceGoFunkyCalledEventArgs sealed
{
public:
    TCDashboardInterfaceGoFunkyCalledEventArgs(_In_ Windows::Devices::AllJoyn::AllJoynMessageInfo^ info);

    property Windows::Devices::AllJoyn::AllJoynMessageInfo^ MessageInfo
    {
        Windows::Devices::AllJoyn::AllJoynMessageInfo^ get() { return m_messageInfo; }
    }

    property TCDashboardInterfaceGoFunkyResult^ Result
    {
        TCDashboardInterfaceGoFunkyResult^ get() { return m_result; }
        void set(_In_ TCDashboardInterfaceGoFunkyResult^ value) { m_result = value; }
    }

    Windows::Foundation::Deferral^ GetDeferral();

    static Windows::Foundation::IAsyncOperation<TCDashboardInterfaceGoFunkyResult^>^ GetResultAsync(TCDashboardInterfaceGoFunkyCalledEventArgs^ args)
    {
        args->InvokeAllFinished();
        auto t = concurrency::create_task(args->m_tce);
        return concurrency::create_async([t]() -> concurrency::task<TCDashboardInterfaceGoFunkyResult^>
        {
            return t;
        });
    }
    
private:
    void Complete();
    void InvokeAllFinished();
    void InvokeCompleteHandler();

    bool m_raised;
    int m_completionsRequired;
    concurrency::task_completion_event<TCDashboardInterfaceGoFunkyResult^> m_tce;
    std::mutex m_lock;
    Windows::Devices::AllJoyn::AllJoynMessageInfo^ m_messageInfo;
    TCDashboardInterfaceGoFunkyResult^ m_result;
};

public ref class TCDashboardInterfaceGoBoringCalledEventArgs sealed
{
public:
    TCDashboardInterfaceGoBoringCalledEventArgs(_In_ Windows::Devices::AllJoyn::AllJoynMessageInfo^ info);

    property Windows::Devices::AllJoyn::AllJoynMessageInfo^ MessageInfo
    {
        Windows::Devices::AllJoyn::AllJoynMessageInfo^ get() { return m_messageInfo; }
    }

    property TCDashboardInterfaceGoBoringResult^ Result
    {
        TCDashboardInterfaceGoBoringResult^ get() { return m_result; }
        void set(_In_ TCDashboardInterfaceGoBoringResult^ value) { m_result = value; }
    }

    Windows::Foundation::Deferral^ GetDeferral();

    static Windows::Foundation::IAsyncOperation<TCDashboardInterfaceGoBoringResult^>^ GetResultAsync(TCDashboardInterfaceGoBoringCalledEventArgs^ args)
    {
        args->InvokeAllFinished();
        auto t = concurrency::create_task(args->m_tce);
        return concurrency::create_async([t]() -> concurrency::task<TCDashboardInterfaceGoBoringResult^>
        {
            return t;
        });
    }
    
private:
    void Complete();
    void InvokeAllFinished();
    void InvokeCompleteHandler();

    bool m_raised;
    int m_completionsRequired;
    concurrency::task_completion_event<TCDashboardInterfaceGoBoringResult^> m_tce;
    std::mutex m_lock;
    Windows::Devices::AllJoyn::AllJoynMessageInfo^ m_messageInfo;
    TCDashboardInterfaceGoBoringResult^ m_result;
};

// Readable Properties
// Writable Properties
} } } 

#ifndef ANDROID_LOGBUFFER_HPP
#define ANDROID_LOGBUFFER_HPP

#include <android/log.h>
#include <ostream>

namespace android
{
    /// @brief: An interface into the a logging/tracing system for XR.
    class logbuffer :
            public std::streambuf
    {
    public :
        static const int bufsize = 2048; // ... or some other suitable buffer size

        logbuffer(android_LogPriority
            priority,
            const std::string &tag);

        logbuffer(android_LogPriority
                  priority,
                  const char* tag);

    private :
        int overflow(int c);

        int sync();

        android_LogPriority priority;
        const char* tag;
        char buffer[bufsize];
    };

    static std::ostream log_verbose(new logbuffer(ANDROID_LOG_VERBOSE, "Native"));
    static std::ostream log_debug(new logbuffer(ANDROID_LOG_DEBUG, "Native"));
    static std::ostream log_info(new logbuffer(ANDROID_LOG_INFO, "Native"));
    static std::ostream log_error(new logbuffer(ANDROID_LOG_ERROR, "Native"));
    static std::ostream log_warning(new logbuffer(ANDROID_LOG_WARN, "Native"));
    static std::ostream log_fatal(new logbuffer(ANDROID_LOG_FATAL, "Native"));
    static std::ostream log_silent(new logbuffer(ANDROID_LOG_SILENT, "Native"));
}

#endif

#include "include/android_logbuffer.h"

namespace android
{
//    logbuffer verbose_buf(ANDROID_LOG_VERBOSE, "Native");
//    logbuffer debug_buf(ANDROID_LOG_DEBUG, "Native");
//    static logbuffer info_buf(ANDROID_LOG_INFO, "Native");
//    static logbuffer error_buf(ANDROID_LOG_ERROR, "Native");
//    static logbuffer warning_buf(ANDROID_LOG_WARN, "Native");
//    static logbuffer fatal_buf(ANDROID_LOG_FATAL, "Native");
//    static logbuffer silent_buf(ANDROID_LOG_FATAL, "Native");

    logbuffer::logbuffer(android_LogPriority p, const std::string &t) :
            priority(p),
            tag(t.c_str())
    {
        this->setp(buffer, buffer + bufsize - 1);
    }

    logbuffer::logbuffer(android_LogPriority p, const char* t) :
            priority(p),
            tag(t)
    {
        this->setp(buffer, buffer + bufsize - 1);
    }

    int logbuffer::overflow(int c)
    {
        if (c == traits_type::eof())
        {
            *this->pptr() = traits_type::to_char_type(c);
            this->sbumpc();
        }

        return this->sync() ? traits_type::eof() : traits_type::not_eof(c);
    }

    int logbuffer::sync()
    {
        int rc = 0;

        if (this->pbase() != this->pptr())
        {
            const char *str = std::string(this->pbase(), this->pptr() - this->pbase()).c_str();

            __android_log_print(priority, tag, "%s", str);

            rc = 0;

            this->setp(buffer, buffer + bufsize - 1);
        }

        return rc;
    }
}

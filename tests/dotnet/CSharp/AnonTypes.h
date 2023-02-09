#ifndef KONAN_EXAMPLE_LIB_H
#define KONAN_EXAMPLE_LIB_H
#ifdef __cplusplus
extern "C" {
#endif
#ifdef __cplusplus
    typedef bool            example_lib_KBoolean;
#else
    typedef _Bool           example_lib_KBoolean;
#endif
    typedef unsigned short     example_lib_KChar;
    typedef signed char        example_lib_KByte;
    typedef short              example_lib_KShort;
    typedef int                example_lib_KInt;
    typedef long long          example_lib_KLong;
    typedef unsigned char      example_lib_KUByte;
    typedef unsigned short     example_lib_KUShort;
    typedef unsigned int       example_lib_KUInt;
    typedef unsigned long long example_lib_KULong;
    typedef float              example_lib_KFloat;
    typedef double             example_lib_KDouble;
    typedef void*              example_lib_KNativePtr;
    struct example_lib_KType;
    typedef struct example_lib_KType example_lib_KType;

    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_APIHost;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_APIHost_CACAHUETE;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_APIHost_CAPI;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_kotlin_collections_Map;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_kotlin_Any;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method_GET;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method_POST;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method_PUT;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method_DELETE;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Method_PATCH;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Response;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Response_DataResponse;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Response_FileResponse;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_com_plangrid_example_Request_Response_ErrorResponse;
    typedef struct {
        example_lib_KNativePtr pinned;
    } example_lib_kref_kotlin_Error;


    typedef struct {
        /* Service functions. */
        void(*DisposeStablePointer)(example_lib_KNativePtr ptr);
        void(*DisposeString)(const char* string);
        example_lib_KBoolean(*IsInstance)(example_lib_KNativePtr ref, const example_lib_KType* type);

        /* User functions. */
        struct {
            struct {
                struct {
                    struct {
                        struct {
                            struct {
                                example_lib_KType* (*_type)(void);
                                struct {
                                    example_lib_kref_com_plangrid_example_APIHost(*get)(); /* enum entry for CACAHUETE. */
                                } CACAHUETE;
                                struct {
                                    example_lib_kref_com_plangrid_example_APIHost(*get)(); /* enum entry for CAPI. */
                                } CAPI;
                            } APIHost;
                            struct {
                                example_lib_KType* (*_type)(void);
                                example_lib_kref_com_plangrid_example_Request(*Request)(const char* path, example_lib_kref_kotlin_collections_Map queryParams, const char* body, example_lib_kref_com_plangrid_example_Request_Method method);
                                const char* (*get_path)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_kref_kotlin_collections_Map(*get_queryParams)(example_lib_kref_com_plangrid_example_Request thiz);
                                const char* (*get_body)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_kref_com_plangrid_example_Request_Method(*get_method)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_KBoolean(*equals)(example_lib_kref_com_plangrid_example_Request thiz, example_lib_kref_kotlin_Any other);
                                example_lib_KInt(*hashCode)(example_lib_kref_com_plangrid_example_Request thiz);
                                const char* (*toString)(example_lib_kref_com_plangrid_example_Request thiz);
                                const char* (*component1)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_kref_kotlin_collections_Map(*component2)(example_lib_kref_com_plangrid_example_Request thiz);
                                const char* (*component3)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_kref_com_plangrid_example_Request_Method(*component4)(example_lib_kref_com_plangrid_example_Request thiz);
                                example_lib_kref_com_plangrid_example_Request(*copy)(example_lib_kref_com_plangrid_example_Request thiz, const char* path, example_lib_kref_kotlin_collections_Map queryParams, const char* body, example_lib_kref_com_plangrid_example_Request_Method method);
                                struct {
                                    example_lib_KType* (*_type)(void);
                                    struct {
                                        example_lib_kref_com_plangrid_example_Request_Method(*get)(); /* enum entry for GET. */
                                    } GET;
                                    struct {
                                        example_lib_kref_com_plangrid_example_Request_Method(*get)(); /* enum entry for POST. */
                                    } POST;
                                    struct {
                                        example_lib_kref_com_plangrid_example_Request_Method(*get)(); /* enum entry for PUT. */
                                    } PUT;
                                    struct {
                                        example_lib_kref_com_plangrid_example_Request_Method(*get)(); /* enum entry for DELETE. */
                                    } DELETE;
                                    struct {
                                        example_lib_kref_com_plangrid_example_Request_Method(*get)(); /* enum entry for PATCH. */
                                    } PATCH;
                                } Method;
                                struct {
                                    example_lib_KType* (*_type)(void);
                                    struct {
                                        example_lib_KType* (*_type)(void);
                                        example_lib_kref_com_plangrid_example_Request_Response_DataResponse(*DataResponse)(const char* data);
                                        const char* (*get_data)(example_lib_kref_com_plangrid_example_Request_Response_DataResponse thiz);
                                    } DataResponse;
                                    struct {
                                        example_lib_KType* (*_type)(void);
                                        example_lib_kref_com_plangrid_example_Request_Response_FileResponse(*FileResponse)(const char* filePath);
                                        const char* (*get_filePath)(example_lib_kref_com_plangrid_example_Request_Response_FileResponse thiz);
                                    } FileResponse;
                                    struct {
                                        example_lib_KType* (*_type)(void);
                                        example_lib_kref_com_plangrid_example_Request_Response_ErrorResponse(*ErrorResponse)(example_lib_kref_kotlin_Error error);
                                        example_lib_kref_kotlin_Error(*get_error)(example_lib_kref_com_plangrid_example_Request_Response_ErrorResponse thiz);
                                    } ErrorResponse;
                                    struct {
                                      int i;
                                    } $serializer;
                                } Response;
                            } Request;
                        } example;
                    } plangrid;
                } com;
            } root;
        } kotlin;
    } example_lib_ExportedSymbols;
    extern example_lib_ExportedSymbols* example_lib_symbols(void);
#ifdef __cplusplus
}  /* extern "C" */
#endif
#endif  /* KONAN_EXAMPLE_LIB_H */

// Code like below would hardly appear in practice
// but it's worth noting we don't support it
#if 0
struct DLL_API TestAnonTypesWithAnonFields
{
public:
    struct
    {
        struct
        {
        };
    };
    struct
    {
        struct
        {
        };
    };
    struct
    {
        struct
        {
        };
    };
    struct
    {
        struct
        {
        };
    };
};
#endif

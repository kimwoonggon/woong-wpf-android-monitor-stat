package com.woong.monitorstack.sync

import java.io.IOException

class AndroidSyncAuthenticationException(
    val statusCode: Int,
    message: String
) : IOException(message)

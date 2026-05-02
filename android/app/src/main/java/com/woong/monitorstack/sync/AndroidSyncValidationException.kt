package com.woong.monitorstack.sync

import java.io.IOException

class AndroidSyncValidationException(
    val statusCode: Int,
    message: String
) : IOException(message)

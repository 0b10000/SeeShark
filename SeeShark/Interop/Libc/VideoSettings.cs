// Copyright (c) The Vignette Authors
// This file is part of SeeShark.
// SeeShark is licensed under the BSD 3-Clause License. See LICENSE for details.

namespace SeeShark.Interop.Libc;

internal enum VideoSettings : int
{
    VIDIOC_QUERYCAP = -2140645888,
    VIDIOC_ENUM_FMT = -1069525502,
    VIDIOC_G_FMT = -1041213948, // previously -1060350460?
    VIDIOC_S_FMT = -1041213947, // previously -1060350459?
    VIDIOC_REQBUFS = -1072409080,
    VIDIOC_QUERYBUF = -1069263351,
    VIDIOC_OVERLAY = 1074025998,
    VIDIOC_QBUF = -1069263345,
    VIDIOC_DQBUF = -1069263343,
    VIDIOC_STREAMON = 1074026002,
    VIDIOC_STREAMOFF = 1074026003,
    VIDIOC_G_PARM = -1060350443,
    VIDIOC_S_PARM = -1060350442,
    VIDIOC_G_CTRL = -1073195493,
    VIDIOC_S_CTRL = -1073195492,
    VIDIOC_QUERYCTRL = -1069263324,
    VIDIOC_G_INPUT = -2147199450,
    VIDIOC_S_INPUT = -1073457625,
    VIDIOC_G_OUTPUT = -2147199442,
    VIDIOC_S_OUTPUT = -1073457617,
    VIDIOC_CROPCAP = -1070836166,
    VIDIOC_G_CROP = -1072409029,
    VIDIOC_S_CROP = 1075074620,
    VIDIOC_TRY_FMT = -1041213888,
    VIDIOC_G_PRIORITY = -2147199421,
    VIDIOC_S_PRIORITY = 1074026052,
    VIDIOC_ENUM_FRAMESIZES = -1070836150,
    VIDIOC_ENUM_FRAMEINTERVALS = -1070311861, // -1069787573 doesn't work
    VIDIOC_PREPARE_BUF = -1069263267,
}

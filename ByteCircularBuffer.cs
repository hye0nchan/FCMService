using System;
using System.Diagnostics;

namespace NetworkService
{
    /**
	 * 버퍼 underflow 예외
	 */
    public class BufferUnderflowException : Exception
    {
    }

    /**
	 * 버퍼 overflow 예외
	 */
    public class BufferOverflowException : Exception
    {
    }

    /**
	 * 원형 byte buffer 큐(Queue) 클래스.
	 * byte 큐에서, pop 이후에 shift 비용을 줄이기 위해 설계되었다.
	 * 동적으로 크기를 변경할 수 없다. 크기 이상을 쓰거나 읽으려고 하면, BufferUnderflowException 또는 BufferOverflowException 이 발생한다.
	 * 
	 * <ul>
	 *  <li>pushXXX 함수들은 큐의 끝에 값을 쓰고, front 를 이동시킨다.</li>
	 *  <li>popXXX 함수들은 큐의 앞쪽에서 값을 읽고, rear 를 이동시킨다.</li>
	 *  <li>getXXX 함수들은 큐의 앞쪽에서 값을 읽지만, rear 를 이동시키지는 않는다.</li>
	 * </ul>
	 * 
	 * <span style="font-weight: bold;">이 클래스는 Thread safe 하게 설계되었다.</span>
	 */
    public class ByteCircularBuffer
    {
        protected byte[] mArray;
        protected int mFront;
        protected int mRear;
        protected readonly bool mIsLittleEndian;
        protected int mCount;

        /**
		 * 새 ByteCircularBuffer 를 만든다.
		 * 
		 * @param capacity
		 *  버퍼의 최대 크기
		 * 
		 * @param isLittleEndian
		 *  버퍼에 값을 읽거나 쓸때, 사용할 byte order 를 지정
		 */
        public ByteCircularBuffer(int capacity, bool isLittleEndian)
        {
            mArray = new byte[capacity];
            mIsLittleEndian = isLittleEndian;
            clear();
        }

        /**
		 * 버퍼의 크기. 버퍼에 저장되어 있는 byte 수.
		 */
        public int size()
        {
            return mCount;
        }

        /**
		 * 큐에서 지정된 byte 의 인덱스를 찾아 리턴한다.
		 * 
		 * @param offset
		 *  큐의 rear 로부터 offset. 검색을 시작할 위치이다.
		 * 
		 * @param value
		 *  검색할 값.
		 *  
		 * @return 값을 찾으면 해당 인덱스(0 <=)를 리턴한다. 찾지 못하면 -1 을 리턴한다.
		 * 
		 * @throw IndexOutOfRangeException offset 이 큐의 범위를 벗어나면 발생한다.
		 */
        public int find(int offset, byte value)
        {
            if (offset < 0 || mCount <= offset)
            {
                throw new IndexOutOfRangeException();
            }

            lock (this)
            {
                int count = mCount;
                int tmpIndex;
                for (int ii = offset; ii < count; ++ii)
                {
                    tmpIndex = (mRear + ii) % mArray.Length;
                    if (value == mArray[tmpIndex])
                    {
                        return ii;
                    }
                }

                return -1;
            }
        }

        /**
		 * 큐에서 지정된 byte[] 의 인덱스를 찾아 리턴한다.
		 * 
		 * @param offset
		 *  큐의 rear 로부터 offset. 검색을 시작할 위치이다.
		 * 
		 * @param value
		 *  검색할 byte[] 값.
		 *  
		 * @return 값을 찾으면 해당 인덱스(0 <=)를 리턴한다. 찾지 못하면 -1 을 리턴한다.
		 * 
		 * @throw IndexOutOfRangeException offset 이 큐의 범위를 벗어나면 발생한다.
		 * @throw ArgumentException 파라메터 값이 비정상적이면 발생한다.
		 */
        public int find(int offset, byte[] value)
        {
            if (null == value || value.Length <= 0)
            {
                throw new ArgumentException();
            }

            lock (this)
            {
                while (offset < mCount)
                {
                    int ret = find(offset, value[0]);
                    if (ret < 0)
                    {
                        return -1;
                    }

                    int cnt = compare(ret, value);
                    if (cnt == value.Length)
                    {
                        return ret;
                    }

                    offset += cnt;
                }

                return -1;
            }
        }

        /**
		 * 큐에서 지정된 byte[] 와 비교한다.
		 * 
		 * @param offset
		 *  큐의 rear 로부터 offset. 비교를 시작할 위치이다.
		 * 
		 * @param value
		 *  비교할 byte[] 값.
		 *  
		 * @return value 의 크기만큼 비교해서, 값이 동일하면 value 의 크기를 리턴한다. 동일하지 않으면, 순차적으로 비교해서 동일한 원소의 개수를 리턴한다.
		 * 
		 * @throw IndexOutOfRangeException offset 이 큐의 범위를 벗어나면 발생한다.
		 * @throw ArgumentException 파라메터 값이 비정상적이면 발생한다.
		 */
        public int compare(int offset, byte[] value)
        {
            if (null == value || value.Length <= 0)
            {
                throw new ArgumentException();
            }

            if (offset < 0 || mCount <= offset + value.Length)
            {
                throw new IndexOutOfRangeException();
            }

            lock (this)
            {
                int count = value.Length;
                int tmpIndex;
                for (int ii = 0; ii < count; ++ii)
                {
                    tmpIndex = (mRear + offset + ii) % mArray.Length;
                    if (value[ii] != mArray[tmpIndex])
                    {
                        return ii;
                    }
                }

                return value.Length;
            }
        }

        /**
		 * 큐의 지정한 위치의 byte 값을 리턴한다. rear 를 이동시키지 않는다.
		 * 
		 * @param offset
		 *  큐의 rear 로부터 offset.
		 *  
		 * @return 인덱스 위치의 byte 값.
		 * 
		 * @throw IndexOutOfRangeException offset 이 큐의 범위를 벗어나면 발생한다.
		 */
        public byte get(int offset)
        {
            if (offset < 0 || mCount <= offset)
            {
                throw new IndexOutOfRangeException();
            }

            lock (this)
            {
                int tmpIndex = (mRear + offset) % mArray.Length;
                return mArray[tmpIndex];
            }
        }

        /**
		 * 큐의 지정한 위치에서 4 byte 를 읽어 int 형태로 리턴한다. rear 를 이동시키지 않는다.
		 * 
		 * @param offset
		 *  큐의 rear 로부터 offset.
		 *  
		 * @return 버퍼에서 읽은 int 값.
		 * 
		 * @throw IndexOutOfRangeException offset 이 큐의 범위를 벗어나면 발생한다.
		 */
        public int getInt(int offset)
        {
            if (mCount < offset + 4)
            {
                throw new IndexOutOfRangeException();
            }

            lock (this)
            {
                int tmpIndex = (mRear + offset) % mArray.Length;
                int value;
                if (mIsLittleEndian)
                {
                    value = mArray[tmpIndex] & 0xff;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | (mArray[tmpIndex] & 0xff) << 8;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | (mArray[tmpIndex] & 0xff) << 16;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | (mArray[tmpIndex] & 0xff) << 24;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;
                }
                else
                {
                    value = (mArray[tmpIndex] & 0xff) << 24;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | (mArray[tmpIndex] & 0xff) << 16;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | (mArray[tmpIndex] & 0xff) << 8;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;

                    value = value | mArray[tmpIndex] & 0xff;
                    tmpIndex = (tmpIndex + 1) % mArray.Length;
                }

                return value;
            }
        }

        /**
		 * 큐의 rear 에서 지정한 byte 수만큼 읽어온다. rear 를 이동시키지 않는다.
		 * 
		 * @param srcOffset
		 *  큐의 rear 로부터 인덱스 offset
		 * 
		 * @param dstArray
		 *  값을 복사할 대상 byte array.
		 * 
		 * @param dstOffset
		 *  dstArray 의 시작 인덱스.
		 * 
		 * @param byteCount
		 *  읽어올 byte 수.
		 * 
		 * @throw IndexOutOfRangeException 읽어올 데이터의 범위가 큐의 범위를 벗어나면 발생한다.
		 */
        public void getBytes(int srcOffset, byte[] dstArray, int dstOffset, int byteCount)
        {
            Debug.Assert(0 <= dstOffset);
            Debug.Assert(dstOffset + byteCount <= dstArray.Length);

            if (srcOffset < 0 || mCount < srcOffset + byteCount)
            {
                throw new IndexOutOfRangeException();
            }

            lock (this)
            {
                int tmpIndex = mRear + srcOffset;

                // 두번에 나누어 복사해야 하는 경우!
                if (tmpIndex < mArray.Length && mArray.Length < tmpIndex + byteCount)
                {
                    int splitSize = mArray.Length - tmpIndex;
                    Array.Copy(mArray, tmpIndex, dstArray, dstOffset, splitSize);
                    Array.Copy(mArray, 0, dstArray, dstOffset + splitSize, byteCount - splitSize);
                }
                else
                {
                    tmpIndex = tmpIndex % mArray.Length;

                    // 한번에 쭉 복사되는 경우
                    Array.Copy(mArray, tmpIndex, dstArray, dstOffset, byteCount);
                }
            }
        }

        /**
		 * 큐의 rear 에서 byte 값을 꺼내온다. rear 를 1 만큼 이동시킨다.
		 * 
		 * @throw BufferUnderflowException 읽어올 값의 크기보다 큐의 크기가 작으면 발생한다.
		 */
        public byte pop()
        {
            if (mCount < 1)
            {
                throw new BufferUnderflowException();
            }

            lock (this)
            {
                byte value = mArray[mRear];
                mRear = (mRear + 1) % mArray.Length;
                --mCount;
                return value;
            }
        }

        /**
		 * 큐의 rear 에서 int 값을 꺼내온다. rear 를 4 만큼 이동시킨다.
		 * 
		 * @throw BufferUnderflowException 읽어올 값의 크기보다 큐의 크기가 작으면 발생한다.
		 */
        public int popInt()
        {
            if (mCount < 4)
            {
                throw new BufferUnderflowException();
            }

            lock (this)
            {
                int value = getInt(0); // 맨 앞에서 꺼낸다.
                mRear = (mRear + 4) % mArray.Length;
                mCount -= 4;

                return value;
            }
        }

        /**
		 * 큐의 rear 에서 지정한 byte 수만큼 꺼내온다. rear 를 이동시킨다.
		 * 
		 * @param dstArray
		 *  데이터를 복사할 대상 byte array.
		 * 
		 * @param dstOffset
		 *  dstArray 의 시작 offset.
		 * 
		 * @param byteCount
		 *  꺼내올 데이터 byte 수.
		 * 
		 * @throw BufferUnderflowException 읽어올 값의 크기보다 큐의 크기가 작으면 발생한다.
		 */
        public void popBytes(byte[] dstArray, int dstOffset, int byteCount)
        {
            Debug.Assert(0 <= dstOffset);
            Debug.Assert(dstOffset + byteCount <= dstArray.Length);

            if (mCount < byteCount)
            {
                throw new BufferUnderflowException();
            }

            lock (this)
            {
                getBytes(0, dstArray, dstOffset, byteCount);

                mRear = (mRear + byteCount) % mArray.Length;
                mCount -= byteCount;
            }
        }

        /**
		 * 큐의 rear 부터 지정한 만큼의 데이터를 버린다. rear 를 이동시킨다.
		 * 
		 * @param byteCount
		 *  버릴 데이터 byte 수.
		 * 
		 * @throw BufferUnderflowException 버릴 byte 수보다 큐의 크기가 작으면 발생한다.
		 */
        public void drawBytes(int byteCount)
        {
            if (mCount < byteCount)
            {
                throw new BufferUnderflowException();
            }

            lock (this)
            {
                mRear = (mRear + byteCount) % mArray.Length;
                mCount -= byteCount;
                // Log.d("ByteCircularBuffer", "Front = " + mFront + ", Rear = " + mRear + ", Count = " + mCount);
            }
        }

        public void clear()
        {
            mFront = mRear = 0;
            mCount = 0;
        }

        /**
		 * 큐의 front 에 byte 값을 쓴다. front 를 1 만큼 이동시킨다.
		 * 
		 * @param value
		 *  쓸 byte 값.
		 * 
		 * @throw BufferOverflowException 버퍼의 크기보다 더 많이 쓰려고 할때 발생한다.
		 */
        public void push(byte value)
        {
            lock (this)
            {
                if (mArray.Length - mCount < 1)
                {
                    throw new BufferOverflowException();
                }

                mArray[mFront] = value;
                mFront = (mFront + 1) % mArray.Length;
                ++mCount;
            }
        }

        /**
		 * 큐의 front 에 int 값을 쓴다. front 를 4 만큼 이동시킨다.
		 * 
		 * @param value
		 *  쓸 int 값.
		 * 
		 * @throw BufferOverflowException 버퍼의 크기보다 더 많이 쓰려고 할때 발생한다.
		 */
        public void pushInt(int value)
        {
            if (mArray.Length - mCount < 4)
            {
                throw new BufferOverflowException();
            }

            lock (this)
            {
                if (mIsLittleEndian)
                {
                    mArray[mFront] = (byte)value;
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)(value >> 8);
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)(value >> 16);
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)(value >> 24);
                    mFront = (mFront + 1) % mArray.Length;
                }
                else
                {
                    mArray[mFront] = (byte)(value >> 24);
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)(value >> 16);
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)(value >> 8);
                    mFront = (mFront + 1) % mArray.Length;

                    mArray[mFront] = (byte)value;
                    mFront = (mFront + 1) % mArray.Length;
                }
                mCount += 4;
            }
        }

        /**
		 * 큐의 front 에 지정한 크기만큼 데이터를 쓴다. front 를 이동시킨다.
		 * 
		 * @param srcArray
		 *  큐에 쓸 데이터 byte array.
		 * 
		 * @param srcOffset
		 *  srcArray 의 시작 offset.
		 * 
		 * @param byteCount
		 *  쓸 데이터 byte 수.
		 * 
		 * @throw BufferOverflowException 버퍼의 크기보다 더 많이 쓰려고 할때 발생한다.
		 */
        public void pushBytes(byte[] srcArray, int srcOffset, int byteCount)
        {
            Debug.Assert(byteCount + srcOffset <= srcArray.Length);

            if (mArray.Length - mCount < byteCount)
            {
                throw new BufferOverflowException();
            }

            lock (this)
            {
                // 한번에 쭉 복사되는 경우
                if (mFront + byteCount <= mArray.Length)
                {
                    Array.Copy(srcArray, srcOffset, mArray, mFront, byteCount);
                }
                else
                {
                    // 두번에 나누어 복사해야 하는 경우!
                    int splitSize = mArray.Length - mFront;
                    Array.Copy(srcArray, srcOffset, mArray, mFront, splitSize);
                    Array.Copy(srcArray, srcOffset + splitSize, mArray, 0, byteCount - splitSize);
                }

                mFront = (mFront + byteCount) % mArray.Length;
                mCount += byteCount;

                // Log.d("ByteCircularBuffer", "Front = " + mFront + ", Rear = " + mRear + ", Count = " + mCount);
            }
        }
    }
}

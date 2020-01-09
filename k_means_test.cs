using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace kmeans
{
    public class k_means_test : MonoBehaviour
    {
        #region Definition
        public Texture srcTex;
        public ComputeShader test_k;
        private RenderTexture testTex;

        public static int cluster_size = 7;//クラスター数
        public double neardistribution = 1;//近いと判定する距離
        public static int count = 0;
        Color[] centroid = new Color[cluster_size];//重心用
        Color[] buffer = new Color[cluster_size];//一つ前の重心
        #endregion Definition

        // Use this for initialization
        void Start()
        {          
            testTex = new RenderTexture(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
            testTex.enableRandomWrite = true;
            testTex.Create();
            ImageProgress();
        }

        //RenderTextureをTexture２Dに変換
        Texture2D CreateTexture2D(RenderTexture rt)
        {
            //Texture2Dを作成
            Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false, false);

            RenderTexture.active = rt;
            texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture2D.Apply();

            //元に戻す別のカメラを用意してそれをRenderTexter用にすれば下のコードはいらないです。
            RenderTexture.active = null;
            Debug.Log("Tex2Dに変換終了");
            return texture2D;
        }

        //平滑化処理
        private void ImageProgress()
        {
            var step1 = test_k.FindKernel("imageprogress");

            test_k.SetTexture(step1, "srcTexture", srcTex);
            test_k.SetTexture(step1, "Result", testTex);
            test_k.Dispatch(step1, srcTex.width / 8, srcTex.height / 8, 1);

            Texture2D texture = CreateTexture2D(testTex);
            //↓は表示用
            //GetComponent<Renderer>().material.mainTexture = texture;

            int[,] map = new int[texture.width, texture.height];
            Debug.Log("平滑化終了");
            Needclasster(texture, map);
        }

        //以下kmeans法==========================================================================================
        //初期値
/*        private void Randinit()
        {
            //int seed = Environment.TickCount;//秒数を保持
            for (int i = 0; i < cluster_size; i++)
            {
                //Random rnd = new Random(seed++);
                centroid[i] = new Color()
                {
                    r = Random.value,
                    g = Random.value,
                    b = Random.value
                };
                    
            }
        }*/

        //重心が停止してるか判断
        private bool ClusterCheck()
        {
            int cnt = 0;
            bool ret = false;

            for (int i = 0; i < cluster_size; i++)
            {
                if (centroid[i].r == buffer[i].r && centroid[i].g == buffer[i].g && centroid[i].b == buffer[i].b)//centroid(今の重心)とbuffer(一つ前の重心)のRGB値が同じなら
                {
                    cnt = cnt + 1;
                    Debug.Log(cnt);
                }
            }
            if (cnt == cluster_size)
            {
                ret = true;
            }

            return ret;
        }

        //画素（Point）と重心の色の距離を計算
        private double ColorDistance(Point a, Color b, Texture2D Image)
        {
            Debug.Log("距離計算開始");
            double dR = Image.GetPixel(a.X, a.Y).r - (int)b.r;
            double dG = Image.GetPixel(a.X, a.Y).g - (int)b.g;
            double dB = Image.GetPixel(a.X, a.Y).b - (int)b.b;

            return Math.Sqrt(dR * dR + dG * dG + dB * dB);
        }

        //kmeans法を行う
        void Needclasster(Texture2D ProgressedImage, int[,] mapp)
        {
            Debug.Log("kmeans突入");
            //Randinit();
            while (ClusterCheck() == false && count < 500)
            {
                Debug.Log("while");
                for (int i = 0; i < ProgressedImage.width; i++)
                {
                    for (int j = 0; j < ProgressedImage.height; j++)
                    {
                        Point img = new Point(i, j);//画素を特定
                        double dist = 0;
                        int place = 0;

                        for (int k = 0; k < cluster_size; k++)
                        {
                            dist = ColorDistance(img, centroid[k], ProgressedImage);//距離計算
                            Debug.Log(dist);
                            if (dist < neardistribution)
                            {
                                neardistribution = dist;
                                place = k;//クラスターを決定
                            }
                        }
                        mapp[i, j] = place;

                    }
                }

                //重心を計算
                double[] sum_R = new double[cluster_size];
                double[] sum_G = new double[cluster_size];
                double[] sum_B = new double[cluster_size];
                int[] num = new int[cluster_size];
                int cntdebug = 0;
                // 重心を計算
                for (int i = 0; i < ProgressedImage.width; i++)
                {
                    for (int j = 0; j < ProgressedImage.height; j++)
                    {
                        sum_R[mapp[i, j]] += ProgressedImage.GetPixel(i, j).r;
                        sum_G[mapp[i, j]] += ProgressedImage.GetPixel(i, j).g;
                        sum_B[mapp[i, j]] += ProgressedImage.GetPixel(i, j).b;
                        num[mapp[i, j]] = num[mapp[i, j]] + 1;
                        cntdebug = cntdebug + 1;
                    }
                }

                for (int k = 0; k < cluster_size; k++)
                {
                    // 前の重心位置を記憶しておく
                    buffer[k] = new Color(centroid[k].r, centroid[k].g, centroid[k].b);

                    if (num[k] == 0)//0なら以下の作業をスキップする
                    {
                        continue;
                    }

                    // 重心位置の更新
                    centroid[k] = new Color((int)sum_R[k] / num[k], (int)sum_G[k] / num[k], (int)sum_B[k] / num[k]);

                }
            }
            Debug.Log("while無視");
            //以下表示用
            Texture2D resltimage = new Texture2D(ProgressedImage.width, ProgressedImage.height);
            for (int i = 0; i < ProgressedImage.width; i++)
            {
                for (int j = 0; j < ProgressedImage.height; j++)
                {
                    resltimage.SetPixel(i, j, centroid[mapp[i, j]]);
                }
            }

            GetComponent<Renderer>().material.mainTexture = resltimage;
        }
    }
}

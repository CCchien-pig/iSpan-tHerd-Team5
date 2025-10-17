/**
 * productMock.js - 商品模組 Mock 資料
 * 功能：提供商品相關的測試資料
 */

import MockAdapter from 'axios-mock-adapter'
import apiClient from '../../core/api/api'
import { isMockEnabled } from './mockConfig'

// 建立 Mock Adapter 實例
const mock = new MockAdapter(apiClient, {
  onNoMatch: 'passthrough', // 沒有符合的請求會繼續發送到真實 API
})

/**
 * 模擬商品列表資料
 */
const mockProductListData = {
  total: 126,
  page: 1,
  pageSize: 20,
  items: [
    {
      productId: 85180,
      productName: 'California Gold Nutrition, Omega 800',
      brandName: 'California Gold Nutrition',
      salePrice: 880,
      listPrice: 1080,
      imageUrl: 'https://picsum.photos/300/300?random=1',
      badge: '熱銷',
      avgRating: 4.8,
      reviewCount: 132,
    },
    {
      productId: 85181,
      productName: 'NOW Foods, Ultra Omega-3',
      brandName: 'NOW Foods',
      salePrice: 599,
      listPrice: 799,
      imageUrl: 'https://picsum.photos/300/300?random=2',
      badge: '新品',
      avgRating: 4.6,
      reviewCount: 89,
    },
    {
      productId: 85182,
      productName: '維生素 D3 5000 IU',
      brandName: "Nature's Bounty",
      salePrice: 299,
      listPrice: 399,
      imageUrl: 'https://picsum.photos/300/300?random=3',
      badge: '特價',
      avgRating: 4.9,
      reviewCount: 256,
    },
    {
      productId: 85183,
      productName: 'Life Extension, 超級泛醇輔酶 Q10',
      brandName: 'Life Extension',
      salePrice: 1280,
      listPrice: 1580,
      imageUrl: 'https://picsum.photos/300/300?random=4',
      badge: '熱銷',
      avgRating: 4.7,
      reviewCount: 198,
    },
    {
      productId: 85184,
      productName: 'Garden of Life, Vitamin Code 女性綜合維他命',
      brandName: 'Garden of Life',
      salePrice: 1099,
      listPrice: 1399,
      imageUrl: 'https://picsum.photos/300/300?random=5',
      badge: '',
      avgRating: 4.8,
      reviewCount: 324,
    },
    {
      productId: 85185,
      productName: 'Jarrow Formulas, 甲基葉酸 400 mcg',
      brandName: 'Jarrow Formulas',
      salePrice: 450,
      listPrice: 650,
      imageUrl: 'https://picsum.photos/300/300?random=6',
      badge: '特價',
      avgRating: 4.6,
      reviewCount: 167,
    },
    {
      productId: 85186,
      productName: 'Solgar, 螯合鎂 200 粒',
      brandName: 'Solgar',
      salePrice: 680,
      listPrice: 880,
      imageUrl: 'https://picsum.photos/300/300?random=7',
      badge: '',
      avgRating: 4.5,
      reviewCount: 143,
    },
    {
      productId: 85187,
      productName: "Doctor's Best, 高吸收薑黃素",
      brandName: "Doctor's Best",
      salePrice: 799,
      listPrice: 999,
      imageUrl: 'https://picsum.photos/300/300?random=8',
      badge: '新品',
      avgRating: 4.7,
      reviewCount: 289,
    },
    {
      productId: 85188,
      productName: 'Nordic Naturals, 兒童 DHA 魚油軟糖',
      brandName: 'Nordic Naturals',
      salePrice: 880,
      listPrice: 1080,
      imageUrl: 'https://picsum.photos/300/300?random=9',
      badge: '熱銷',
      avgRating: 4.9,
      reviewCount: 456,
    },
    {
      productId: 85189,
      productName: 'Thorne Research, 基礎營養配方',
      brandName: 'Thorne Research',
      salePrice: 1580,
      listPrice: 1880,
      imageUrl: 'https://picsum.photos/300/300?random=10',
      badge: '',
      avgRating: 4.8,
      reviewCount: 234,
    },
    {
      productId: 85190,
      productName: 'MegaFood, 血糖平衡配方',
      brandName: 'MegaFood',
      salePrice: 999,
      listPrice: 1299,
      imageUrl: 'https://picsum.photos/300/300?random=21',
      badge: '',
      avgRating: 4.6,
      reviewCount: 178,
    },
    {
      productId: 85191,
      productName: 'Pure Encapsulations, 益生菌 G.I.',
      brandName: 'Pure Encapsulations',
      salePrice: 1380,
      listPrice: 1680,
      imageUrl: 'https://picsum.photos/300/300?random=22',
      badge: '熱銷',
      avgRating: 4.7,
      reviewCount: 267,
    },
    {
      productId: 85192,
      productName: 'Bluebonnet Nutrition, 鈣鎂鋅複合配方',
      brandName: 'Bluebonnet Nutrition',
      salePrice: 720,
      listPrice: 920,
      imageUrl: 'https://picsum.photos/300/300?random=23',
      badge: '特價',
      avgRating: 4.5,
      reviewCount: 201,
    },
    {
      productId: 85193,
      productName: 'Natrol, 褪黑激素 5mg 快速溶解',
      brandName: 'Natrol',
      salePrice: 380,
      listPrice: 480,
      imageUrl: 'https://picsum.photos/300/300?random=24',
      badge: '',
      avgRating: 4.4,
      reviewCount: 389,
    },
    {
      productId: 85194,
      productName: 'Kirkman Labs, 兒童多種維生素',
      brandName: 'Kirkman Labs',
      salePrice: 890,
      listPrice: 1090,
      imageUrl: 'https://picsum.photos/300/300?random=25',
      badge: '',
      avgRating: 4.6,
      reviewCount: 156,
    },
    {
      productId: 85195,
      productName: 'Source Naturals, 葉黃素護眼配方',
      brandName: 'Source Naturals',
      salePrice: 680,
      listPrice: 880,
      imageUrl: 'https://picsum.photos/300/300?random=26',
      badge: '新品',
      avgRating: 4.7,
      reviewCount: 223,
    },
    {
      productId: 85196,
      productName: 'Rainbow Light, 產前綜合維他命',
      brandName: 'Rainbow Light',
      salePrice: 1180,
      listPrice: 1480,
      imageUrl: 'https://picsum.photos/300/300?random=27',
      badge: '熱銷',
      avgRating: 4.8,
      reviewCount: 412,
    },
    {
      productId: 85197,
      productName: 'Sports Research, 有機椰子油 MCT',
      brandName: 'Sports Research',
      salePrice: 799,
      listPrice: 999,
      imageUrl: 'https://picsum.photos/300/300?random=28',
      badge: '',
      avgRating: 4.5,
      reviewCount: 178,
    },
    {
      productId: 85198,
      productName: 'Carlson Labs, 挪威深海魚肝油',
      brandName: 'Carlson Labs',
      salePrice: 950,
      listPrice: 1150,
      imageUrl: 'https://picsum.photos/300/300?random=29',
      badge: '',
      avgRating: 4.6,
      reviewCount: 234,
    },
    {
      productId: 85199,
      productName: "Nature's Way, 接骨木果糖漿",
      brandName: "Nature's Way",
      salePrice: 580,
      listPrice: 780,
      imageUrl: 'https://picsum.photos/300/300?random=30',
      badge: '特價',
      avgRating: 4.9,
      reviewCount: 567,
    },
  ],
}

/**
 * 模擬商品詳細資料
 */
const mockProductDetailData = {
  productId: 85180,
  productName: 'California Gold Nutrition, Omega 800 超高濃縮魚油',
  brandName: 'California Gold Nutrition',
  badge: '熱銷',
  shortDesc: '高濃縮魚油，每顆含 1000mg...',
  fullDesc: '每顆軟膠囊含有高純度魚油，富含 EPA 和 DHA，有助於心血管健康。',
  weight: 0.15,
  volumeCubicMeter: 0.001,
  shipWeight: 0.15,
  expiryDate: '2028-06-01',
  firstListedDate: '2018-09-13',
  upcCode: '898220012664',
  packageQuantity: '90 顆',
  dimensions: '11.3 x 6.2 x 6.2 cm',
  images: [
    { imageId: 1001, fileUrl: 'https://picsum.photos/600/600?random=1', isMain: true },
    { imageId: 1002, fileUrl: 'https://picsum.photos/600/600?random=2', isMain: false },
    { imageId: 1003, fileUrl: 'https://picsum.photos/600/600?random=3', isMain: false },
  ],
  Specs: [
    {
      SkuId: 1001,
      OptionName: '90 顆裝',
      UnitPrice: 1080,
      SalePrice: 880,
      IsActive: true,
    },
    {
      SkuId: 1002,
      OptionName: '180 顆裝',
      UnitPrice: 1980,
      SalePrice: 1580,
      IsActive: true,
    },
  ],
  attributes: [
    { attributeName: '功效', optionName: '心血管保健' },
    { attributeName: '性別', optionName: '通用' },
    { attributeName: '年齡', optionName: '成人' },
  ],
  ingredients: [
    { ingredientName: '魚油', percentage: 1000, note: 'EPA 480mg, DHA 320mg' },
    { ingredientName: '維生素E', percentage: 5, note: '抗氧化劑' },
  ],
  reviewsSummary: {
    avgRating: 4.8,
    reviewCount: 132,
  },
}

/**
 * 模擬屬性清單資料
 */
const mockAttributesData = [
  {
    attributeId: 1000,
    attributeName: '功效',
    options: [
      { optionId: 2000, optionName: '心血管健康' },
      { optionId: 2001, optionName: '免疫調節' },
      { optionId: 2002, optionName: '骨骼保健' },
      { optionId: 2003, optionName: '視力保健' },
      { optionId: 2004, optionName: '腦部健康' },
      { optionId: 2005, optionName: '消化系統' },
      { optionId: 2006, optionName: '皮膚美容' },
      { optionId: 2007, optionName: '運動表現' },
      { optionId: 2008, optionName: '睡眠品質' },
      { optionId: 2009, optionName: '壓力管理' },
    ],
  },
  {
    attributeId: 1001,
    attributeName: '性別',
    options: [
      { optionId: 2010, optionName: '男性' },
      { optionId: 2011, optionName: '女性' },
      { optionId: 2012, optionName: '通用' },
    ],
  },
  {
    attributeId: 1002,
    attributeName: '年齡',
    options: [
      { optionId: 2020, optionName: '嬰幼兒 (0-3歲)' },
      { optionId: 2021, optionName: '兒童 (4-12歲)' },
      { optionId: 2022, optionName: '青少年 (13-17歲)' },
      { optionId: 2023, optionName: '成人 (18-64歲)' },
      { optionId: 2024, optionName: '銀髮族 (65歲以上)' },
    ],
  },
  {
    attributeId: 1003,
    attributeName: '劑型',
    options: [
      { optionId: 2030, optionName: '膠囊' },
      { optionId: 2031, optionName: '錠劑' },
      { optionId: 2032, optionName: '軟膠囊' },
      { optionId: 2033, optionName: '液體' },
      { optionId: 2034, optionName: '粉末' },
      { optionId: 2035, optionName: '軟糖' },
      { optionId: 2036, optionName: '咀嚼錠' },
    ],
  },
  {
    attributeId: 1004,
    attributeName: '飲食偏好',
    options: [
      { optionId: 2040, optionName: '素食' },
      { optionId: 2041, optionName: '純素' },
      { optionId: 2042, optionName: '無麩質' },
      { optionId: 2043, optionName: '非基因改造' },
      { optionId: 2044, optionName: '有機' },
      { optionId: 2045, optionName: '猶太認證' },
      { optionId: 2046, optionName: '清真認證' },
    ],
  },
  {
    attributeId: 1005,
    attributeName: '品牌',
    options: [
      { optionId: 2050, optionName: 'California Gold Nutrition' },
      { optionId: 2051, optionName: 'NOW Foods' },
      { optionId: 2052, optionName: 'Life Extension' },
      { optionId: 2053, optionName: 'Garden of Life' },
      { optionId: 2054, optionName: 'Jarrow Formulas' },
      { optionId: 2055, optionName: 'Solgar' },
      { optionId: 2056, optionName: "Doctor's Best" },
      { optionId: 2057, optionName: 'Nordic Naturals' },
      { optionId: 2058, optionName: 'Thorne Research' },
      { optionId: 2059, optionName: "Nature's Bounty" },
      { optionId: 2060, optionName: 'MegaFood' },
      { optionId: 2061, optionName: 'Pure Encapsulations' },
      { optionId: 2062, optionName: 'Bluebonnet Nutrition' },
      { optionId: 2063, optionName: 'Natrol' },
      { optionId: 2064, optionName: 'Kirkman Labs' },
    ],
  },
]

/**
 * 模擬問答資料
 */
const mockQuestionsData = [
  {
    questionId: 3001,
    questionContent: '懷孕可以吃嗎？',
    createdDate: '2025-09-01',
    userName: '匿名會員',
    answers: [
      {
        answerContent: '建議先詢問醫師。',
        isOfficial: true,
        createdDate: '2025-09-02',
      },
    ],
  },
  {
    questionId: 3002,
    questionContent: '請問保存期限是多久？',
    createdDate: '2025-09-15',
    userName: 'User123',
    answers: [
      {
        answerContent: '保存期限標示在瓶身上，通常為 2 年。',
        isOfficial: true,
        createdDate: '2025-09-16',
      },
    ],
  },
  {
    questionId: 3003,
    questionContent: '請問這款魚油是rTG型、還是TG型、還是EE型？',
    createdDate: '2024-04-27',
    userName: 'Wei',
    answers: [
      {
        answerContent:
          'California Gold Nutrition Omega 800 醫級魚油使用的是 rTG（重新酯化甘油三酯）形式的魚油。rTG 魚油是一種經過加工的魚油，將魚油中的脂肪酸重新酯化成甘油三酯形式，因此 rTG 魚油也被稱為重新酯化魚油。',
        isOfficial: true,
        createdDate: '2024-04-28',
      },
      {
        answerContent: '感謝解答！我也一直想知道這個問題。',
        isOfficial: false,
        createdDate: '2024-04-29',
      },
    ],
  },
  {
    questionId: 3004,
    questionContent: '一天要吃幾顆？',
    createdDate: '2025-08-20',
    userName: 'Chen123',
    answers: [
      {
        answerContent: '建議每天1-2顆，隨餐服用效果更佳。實際用量請依個人需求調整或諮詢專業醫師。',
        isOfficial: true,
        createdDate: '2025-08-21',
      },
    ],
  },
  {
    questionId: 3005,
    questionContent: '可以跟其他保健品一起吃嗎？',
    createdDate: '2025-07-15',
    userName: 'Mary_Lin',
    answers: [
      {
        answerContent:
          '一般來說，魚油可以與大多數保健品一起服用。但如果您正在服用抗凝血藥物或其他處方藥，請先諮詢醫師。',
        isOfficial: true,
        createdDate: '2025-07-16',
      },
    ],
  },
  {
    questionId: 3006,
    questionContent: '小孩可以吃嗎？幾歲以上適合？',
    createdDate: '2025-06-10',
    userName: '關心寶寶的媽媽',
    answers: [
      {
        answerContent:
          '這款魚油適合成人使用。如果要給兒童補充魚油，建議選擇專門為兒童設計的產品，劑量會比較適合。通常建議3歲以上兒童在醫師或營養師指導下使用。',
        isOfficial: true,
        createdDate: '2025-06-11',
      },
      {
        answerContent: '我家小孩5歲，醫生說可以吃成人的，但要減半劑量。',
        isOfficial: false,
        createdDate: '2025-06-12',
      },
    ],
  },
  {
    questionId: 3007,
    questionContent: '有腥味嗎？會打嗝有魚味嗎？',
    createdDate: '2025-05-05',
    userName: '怕腥味的人',
    answers: [
      {
        answerContent:
          '這款魚油採用高品質提煉技術，大大降低了腥味。膠囊外層也有特殊塗層，可以減少打嗝時的魚腥味。不過每個人的感受不同，建議隨餐服用可以進一步降低這種情況。',
        isOfficial: true,
        createdDate: '2025-05-06',
      },
    ],
  },
  {
    questionId: 3008,
    questionContent: '需要冷藏保存嗎？',
    createdDate: '2025-04-18',
    userName: 'Storage_Question',
    answers: [
      {
        answerContent:
          '不需要冷藏，但建議存放在陰涼乾燥處，避免陽光直射。開封後請儘快使用完畢，並確保瓶蓋緊閉以防止氧化。',
        isOfficial: true,
        createdDate: '2025-04-19',
      },
    ],
  },
  {
    questionId: 3009,
    questionContent: '這款跟其他品牌的魚油有什麼差別？',
    createdDate: '2025-03-22',
    userName: '比較控',
    answers: [
      {
        answerContent:
          'California Gold Nutrition 的特色是高濃度配方（800mg Omega-3），採用 rTG 型態，吸收率較好。並且通過第三方檢測，確保無重金屬污染。性價比也相當不錯。',
        isOfficial: true,
        createdDate: '2025-03-23',
      },
      {
        answerContent: '我之前吃過好幾個品牌，這款的CP值真的很高！',
        isOfficial: false,
        createdDate: '2025-03-24',
      },
    ],
  },
  {
    questionId: 3010,
    questionContent: '素食者可以吃嗎？',
    createdDate: '2025-02-14',
    userName: 'Vegan_Life',
    answers: [
      {
        answerContent:
          '這款魚油來自魚類，不適合純素食者。如果您是素食者，建議選擇藻油（植物性 DHA/EPA）作為替代品。',
        isOfficial: true,
        createdDate: '2025-02-15',
      },
    ],
  },
  {
    questionId: 3011,
    questionContent: '吃了會有副作用嗎？',
    createdDate: '2025-01-08',
    userName: '謹慎的消費者',
    answers: [
      {
        answerContent:
          '魚油一般來說是很安全的保健品。少數人可能會有輕微的胃部不適或打嗝。如果您有在服用抗凝血藥物、即將手術或有特殊疾病，請先諮詢醫師。',
        isOfficial: true,
        createdDate: '2025-01-09',
      },
    ],
  },
  {
    questionId: 3012,
    questionContent: '膠囊大小如何？好吞嗎？',
    createdDate: '2024-12-20',
    userName: '不會吞藥的人',
    answers: [
      {
        answerContent:
          '膠囊大小屬於中等，長度約2公分左右。對大部分人來說不會太難吞。如果真的有困難，可以隨餐配大量水服用，或考慮選擇液態魚油。',
        isOfficial: true,
        createdDate: '2024-12-21',
      },
      {
        answerContent: '我覺得還好耶，不會很大顆，很容易吞下去。',
        isOfficial: false,
        createdDate: '2024-12-22',
      },
    ],
  },
  {
    questionId: 3013,
    questionContent: '有通過什麼檢驗認證嗎？',
    createdDate: '2024-11-15',
    userName: '品質控',
    answers: [
      {
        answerContent:
          '本產品通過第三方實驗室檢測，包括重金屬檢測（汞、鉛、鎘等）、氧化值檢測、以及純度檢測。符合國際魚油品質標準（如 GOED、IFOS 標準）。',
        isOfficial: true,
        createdDate: '2024-11-16',
      },
    ],
  },
  {
    questionId: 3014,
    questionContent: '跟醫院賣的處方魚油有什麼不同？',
    createdDate: '2024-10-28',
    userName: 'Doctor_Ask',
    answers: [
      {
        answerContent:
          '處方魚油通常 EPA+DHA 濃度更高（可達 90% 以上），並且是經過藥品級別的嚴格審查。這款保健品魚油濃度雖然較處方魚油低，但對於日常保健已經足夠，且價格更親民。如有特殊疾病需求，建議使用處方魚油。',
        isOfficial: true,
        createdDate: '2024-10-29',
      },
    ],
  },
  {
    questionId: 3015,
    questionContent: '晚上吃還是早上吃比較好？',
    createdDate: '2024-09-12',
    userName: '時間管理大師',
    answers: [
      {
        answerContent:
          '魚油建議隨餐服用即可，早晚都可以。重點是要跟含有脂肪的食物一起吃，這樣吸收效果會更好。選擇自己最方便記得的時間，養成固定習慣最重要。',
        isOfficial: true,
        createdDate: '2024-09-13',
      },
      {
        answerContent: '我都是晚餐後吃，已經養成習慣了！',
        isOfficial: false,
        createdDate: '2024-09-14',
      },
    ],
  },
]

/**
 * 模擬評價資料
 */
const mockReviewsData = [
  {
    reviewId: 5001,
    rating: 5,
    title: '給3讓小孩補充的魚油',
    content:
      '經前處傳說分享推薦，魚油對小孩的大腦發育很有幫助，每天餐後都會拿1條～低劑量較輕盒、只是天對紅椒會使膠囊，沒什麼本身！',
    userName: 'iHerb 客戶',
    createdDate: '2025-09-01',
    images: [{ imageUrl: 'https://picsum.photos/400/400?random=11' }],
    helpfulCount: 136,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5002,
    rating: 1,
    title: '品質差異太大',
    content: '第二次購買，但不久之前第的顏色深了其差，運輸藥也沒什麼不好',
    userName: 'iHerb 客戶',
    createdDate: '2023-03-07',
    images: [],
    helpfulCount: 19,
    unhelpfulCount: 1,
  },
  {
    reviewId: 5003,
    rating: 5,
    title: '屆回購小孩補充魚油',
    content:
      '這款800倍高濃縮Omega-3魚油很有感，遺憾800件商品Omega-3魚油很溼後了許多顯著的好評，其主要放做包括高EPA/DHA含量，有助於心血管管理，減少炎症和聽器預防疫症助，並具有抗發炎擴展與更好雙，還合有敬都每局',
    userName: 'iHerb 客戶',
    createdDate: '2024-05-23',
    images: [
      { imageUrl: 'https://picsum.photos/400/400?random=12' },
      { imageUrl: 'https://picsum.photos/400/400?random=13' },
    ],
    helpfulCount: 79,
    unhelpfulCount: 2,
  },
  {
    reviewId: 5004,
    rating: 5,
    title: '很好吞',
    content: '吃了兩週有感覺！魚油沒有腥味，會持續購買。非常推薦給需要補充Omega-3的朋友。',
    userName: 'Yin**',
    createdDate: '2025-10-10',
    images: [{ imageUrl: 'https://picsum.photos/400/400?random=14' }],
    helpfulCount: 45,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5005,
    rating: 4,
    title: '效果不錯',
    content: '價格合理，品質很好。服用一個月後感覺精神狀態有改善。',
    userName: 'Chen**',
    createdDate: '2025-10-05',
    images: [],
    helpfulCount: 23,
    unhelpfulCount: 1,
  },
  {
    reviewId: 5006,
    rating: 5,
    title: '非常滿意',
    content: '這是我買過最好的魚油，完全沒有腥味，而且吞嚥容易。包裝也很精美。',
    userName: 'Lin**',
    createdDate: '2025-09-28',
    images: [
      { imageUrl: 'https://picsum.photos/400/400?random=15' },
      { imageUrl: 'https://picsum.photos/400/400?random=16' },
    ],
    helpfulCount: 67,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5007,
    rating: 4,
    title: '持續回購中',
    content: '已經是第三次購買了，品質穩定，價格實惠。全家人都在吃。',
    userName: 'Wang**',
    createdDate: '2025-09-15',
    images: [],
    helpfulCount: 34,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5008,
    rating: 5,
    title: '物超所值',
    content: '高濃度魚油，EPA和DHA含量都很高，對心血管健康很有幫助。',
    userName: 'Liu**',
    createdDate: '2025-09-10',
    images: [{ imageUrl: 'https://picsum.photos/400/400?random=17' }],
    helpfulCount: 56,
    unhelpfulCount: 1,
  },
  {
    reviewId: 5009,
    rating: 5,
    title: '醫生推薦',
    content: '我的家庭醫生推薦這款魚油，說品質很好。吃了之後確實感覺不錯。',
    userName: 'Huang**',
    createdDate: '2025-08-28',
    images: [],
    helpfulCount: 89,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5010,
    rating: 4,
    title: '不錯的選擇',
    content: '魚油純度高，沒有雜質。唯一小缺點是膠囊有點大，不過還能接受。',
    userName: 'Zhang**',
    createdDate: '2025-08-20',
    images: [],
    helpfulCount: 28,
    unhelpfulCount: 2,
  },
  {
    reviewId: 5011,
    rating: 5,
    title: '改善了我的健康狀況',
    content: '服用三個月後，體檢報告顯示三酸甘油脂有明顯下降。真的很有效！',
    userName: 'Wu**',
    createdDate: '2025-08-15',
    images: [{ imageUrl: 'https://picsum.photos/400/400?random=18' }],
    helpfulCount: 102,
    unhelpfulCount: 1,
  },
  {
    reviewId: 5012,
    rating: 5,
    title: '全家人的選擇',
    content: '從爸媽到小孩都在吃這款魚油，品質可靠，價格合理。',
    userName: 'Xu**',
    createdDate: '2025-08-05',
    images: [],
    helpfulCount: 41,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5013,
    rating: 3,
    title: '還可以',
    content: '效果一般，但也沒有什麼副作用。繼續觀察看看。',
    userName: 'Zheng**',
    createdDate: '2025-07-28',
    images: [],
    helpfulCount: 12,
    unhelpfulCount: 8,
  },
  {
    reviewId: 5014,
    rating: 5,
    title: '強力推薦',
    content: '我是營養師，這款魚油的配方和純度都很優秀，值得推薦給客戶。',
    userName: 'Dr. Lee**',
    createdDate: '2025-07-20',
    images: [],
    helpfulCount: 156,
    unhelpfulCount: 0,
  },
  {
    reviewId: 5015,
    rating: 4,
    title: '品質優良',
    content: '魚油新鮮，沒有氧化的味道。包裝密封性很好。',
    userName: 'Liao**',
    createdDate: '2025-07-15',
    images: [{ imageUrl: 'https://picsum.photos/400/400?random=19' }],
    helpfulCount: 38,
    unhelpfulCount: 1,
  },
]

/**
 * 註冊商品相關的 Mock API
 */
export function setupProductMock() {
  // ==================== 商品列表 ====================
  if (isMockEnabled('product', 'getProductList')) {
    mock.onGet(/\/prod\/products(\?.*)?$/).reply((config) => {
      // 根據參數篩選資料（簡單示範）
      let items = [...mockProductListData.items]
      const { keyword, page = 1, pageSize = 20 } = config.params || {}

      if (keyword) {
        items = items.filter(
          (item) => item.productName.includes(keyword) || item.brandName.includes(keyword)
        )
      }

      return [
        200,
        {
          success: true,
          message: '',
          data: {
            ...mockProductListData,
            items,
            total: items.length,
            page: parseInt(page),
            pageSize: parseInt(pageSize),
          },
        },
      ]
    })
  }

  // ==================== 商品詳細 ====================
  if (isMockEnabled('product', 'getProductDetail')) {
    mock.onGet(/\/prod\/products\/\d+$/).reply((config) => {
      const productId = parseInt(config.url.split('/').pop())

      return [
        200,
        {
          success: true,
          message: '',
          data: {
            ...mockProductDetailData,
            productId,
          },
        },
      ]
    })
  }

  // ==================== 屬性清單 ====================
  if (isMockEnabled('product', 'getAttributes')) {
    mock.onGet('/prod/attributes').reply(() => {
      return [
        200,
        {
          success: true,
          message: '',
          data: mockAttributesData,
        },
      ]
    })
  }

  // ==================== 成分清單 ====================
  if (isMockEnabled('product', 'getIngredients')) {
    mock.onGet('/prod/ingredients').reply(() => {
      return [
        200,
        {
          success: true,
          message: '',
          data: [
            { ingredientId: 1, ingredientName: '魚油 (EPA/DHA)' },
            { ingredientId: 2, ingredientName: '維生素 A' },
            { ingredientId: 3, ingredientName: '維生素 C' },
            { ingredientId: 4, ingredientName: '維生素 D3' },
            { ingredientId: 5, ingredientName: '維生素 E' },
            { ingredientId: 6, ingredientName: '維生素 K2' },
            { ingredientId: 7, ingredientName: '維生素 B 群' },
            { ingredientId: 8, ingredientName: '葉酸' },
            { ingredientId: 9, ingredientName: '鈣' },
            { ingredientId: 10, ingredientName: '鎂' },
            { ingredientId: 11, ingredientName: '鋅' },
            { ingredientId: 12, ingredientName: '鐵' },
            { ingredientId: 13, ingredientName: '硒' },
            { ingredientId: 14, ingredientName: '碘' },
            { ingredientId: 15, ingredientName: '輔酶 Q10' },
            { ingredientId: 16, ingredientName: '葉黃素' },
            { ingredientId: 17, ingredientName: '玉米黃素' },
            { ingredientId: 18, ingredientName: '益生菌' },
            { ingredientId: 19, ingredientName: '薑黃素' },
            { ingredientId: 20, ingredientName: '白藜蘆醇' },
            { ingredientId: 21, ingredientName: '膠原蛋白' },
            { ingredientId: 22, ingredientName: '透明質酸' },
            { ingredientId: 23, ingredientName: '葡萄糖胺' },
            { ingredientId: 24, ingredientName: '軟骨素' },
            { ingredientId: 25, ingredientName: '褪黑激素' },
            { ingredientId: 26, ingredientName: '酪蛋白' },
            { ingredientId: 27, ingredientName: '乳清蛋白' },
            { ingredientId: 28, ingredientName: '膳食纖維' },
            { ingredientId: 29, ingredientName: '南瓜籽油' },
            { ingredientId: 30, ingredientName: '月見草油' },
          ],
        },
      ]
    })
  }

  // ==================== 問答列表 ====================
  if (isMockEnabled('product', 'getQuestions')) {
    mock.onGet(/\/prod\/questions\/\d+$/).reply((config) => {
      const productId = parseInt(config.url.split('/').pop())

      return [
        200,
        {
          success: true,
          message: '',
          data: mockQuestionsData,
        },
      ]
    })
  }

  // ==================== 提交問題 ====================
  if (isMockEnabled('product', 'submitQuestion')) {
    mock.onPost('/prod/questions').reply((config) => {
      const data = JSON.parse(config.data)

      return [
        200,
        {
          success: true,
          message: '問題已送出',
          data: {
            questionId: Date.now(),
            ...data,
            createdDate: new Date().toISOString().split('T')[0],
          },
        },
      ]
    })
  }

  // ==================== 回覆問題 ====================
  if (isMockEnabled('product', 'submitAnswer')) {
    mock.onPost('/prod/answers').reply((config) => {
      const data = JSON.parse(config.data)

      return [
        200,
        {
          success: true,
          message: '回覆已送出',
          data: {
            answerId: Date.now(),
            ...data,
            createdDate: new Date().toISOString().split('T')[0],
          },
        },
      ]
    })
  }

  // ==================== 評價列表 ====================
  if (isMockEnabled('product', 'getReviews')) {
    mock.onGet(/\/prod\/reviews\/\d+$/).reply((config) => {
      const productId = parseInt(config.url.split('/').pop())

      return [
        200,
        {
          success: true,
          message: '',
          data: mockReviewsData,
        },
      ]
    })
  }

  // ==================== 提交評價 ====================
  if (isMockEnabled('product', 'submitReview')) {
    mock.onPost('/prod/reviews').reply((config) => {
      const data = JSON.parse(config.data)

      return [
        200,
        {
          success: true,
          message: '評價已送出',
          data: {
            reviewId: Date.now(),
            ...data,
            userName: 'User***',
            createdDate: new Date().toISOString().split('T')[0],
          },
        },
      ]
    })
  }

  // ==================== 收藏商品 ====================
  if (isMockEnabled('product', 'addFavorite')) {
    mock.onPost('/prod/favorite').reply((config) => {
      const data = JSON.parse(config.data)

      return [
        200,
        {
          success: true,
          message: '已加入收藏',
          data: data,
        },
      ]
    })
  }

  // ==================== 取消收藏 ====================
  if (isMockEnabled('product', 'removeFavorite')) {
    mock.onDelete(/\/prod\/favorite\/\d+$/).reply((config) => {
      const productId = parseInt(config.url.split('/').pop())

      return [
        200,
        {
          success: true,
          message: '已取消收藏',
          data: { productId },
        },
      ]
    })
  }

  // ==================== 收藏清單 ====================
  if (isMockEnabled('product', 'getFavoriteList')) {
    mock.onGet('/prod/favorite').reply(() => {
      return [
        200,
        {
          success: true,
          message: '',
          data: {
            total: 5,
            items: mockProductListData.items.slice(0, 2),
          },
        },
      ]
    })
  }

  // ==================== 按讚商品 ====================
  if (isMockEnabled('product', 'likeProduct')) {
    mock.onPost('/prod/like').reply((config) => {
      const data = JSON.parse(config.data)

      return [
        200,
        {
          success: true,
          message: '已按讚',
          data: data,
        },
      ]
    })
  }

  // ==================== 取消按讚 ====================
  if (isMockEnabled('product', 'unlikeProduct')) {
    mock.onDelete(/\/prod\/like\/\d+$/).reply((config) => {
      const productId = parseInt(config.url.split('/').pop())

      return [
        200,
        {
          success: true,
          message: '已取消按讚',
          data: { productId },
        },
      ]
    })
  }
}

export default {
  setupProductMock,
}

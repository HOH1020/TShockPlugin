# EconomicsAPI 插件[经济套件前置]

- 作者: 少司命
- 出处: 无
- 经济套件前置插件

## 更新日志

```
暂无
```

## 指令

| 语法                        |           权限           |   说明   |
| --------------------------- | :----------------------: | :------: |
| /bank add [玩家名称] [数量] |      economics.bank      | 添加货币 |
| /bank del [玩家名称] [数量] |      economics.bank      | 删除货币 |
| /bank pay [玩家名称] [数量] |    economics.bank.pay    | 转账货币 |
| /查询                       | economics.currency.query | 查询货币 |

## 配置

```json
{
  "货币名称": "魂力",
  "货币转换率": 1.0,
  "保存时间间隔": 30,
  "显示收益": true,
  "禁用雕像": false,
  "死亡掉落率": 0.0,
  "查询提示": "[c/FFA500:你当前拥有{0}{1}个]"
}
```
## 反馈
- 共同维护的插件库：https://github.com/Controllerdestiny/TShockPlugin
- 国内社区trhub.cn 或 TShock官方群等
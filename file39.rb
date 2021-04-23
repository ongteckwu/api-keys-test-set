class OrdersController < ApplicationController
  before_action :nologin_user, only: [:index]
  before_action :limit_user, only: [:index]
  before_action :no_item, only: [:index]
  before_action :item_set, only: [:index, :create]
 

  def index
    @order = OrderAddress.new
  end

  def create
    @order =  OrderAddress.new(order_params)
    if @order.valid?
      pay_item
      @order.save
      redirect_to root_path
    else
      render :index
    end
  end

  private

  def order_params
    params.require(:order_address).permit(:prefecture_id, :postal_code, :city, :house_number, :building, :phone).merge(user_id: current_user.id, item_id: params[:item_id], token: params[:token])
  end

  def pay_item
    Payjp.api_key = "C5510T5CXYKWYCY00RHHMM2HCY0411M200XCEUE4H4WED0Y5TXT22DEKD1H55HK"
    Payjp::Charge.create(
      amount: @item.price,
      card: order_params[:token],
      currency:'jpy'
    )
  end

  def limit_user
    @item = Item.find(params[:item_id])
    if current_user.id == @item.user_id
      redirect_to root_path
    end
  end

  def nologin_user
    redirect_to user_session_path unless user_signed_in?
  end

  def no_item
    if @item.order != nil
      redirect_to root_path
    end
  end

  def item_set
    @item = Item.find(params[:item_id])
  end
end
